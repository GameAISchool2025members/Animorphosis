import socket
import sounddevice as sd
import numpy as np
import tensorflow as tf
import time
import librosa
import os
import threading
import queue

from collections import deque, Counter
from dotenv import load_dotenv

load_dotenv()
# ---- CONFIG ----
MODEL_PATH = os.getenv("MODEL_PATH", "../soundclassifier_with_metadata.tflite")
LABELS_PATH = os.getenv("LABELS_PATH", "../labels.txt")
UDP_IP = os.getenv("UDP_IP", "127.0.0.1")
UDP_PORT = int(os.getenv("UDP_PORT", "5005"))
SAMPLE_RATE = 16000
EXPECTED_INPUT_SIZE = 44032
DURATION = EXPECTED_INPUT_SIZE / SAMPLE_RATE  # Calculate duration to match expected input size
VAD_MODE = 2  # 0-3, higher = more aggressive
CONFIDENCE_THRESHOLD = 0.8 # Lowered for testing - minimum confidence for classification
VAD_FRAME_DURATION = 30  # ms
PROCESS_INTERVAL = 0.5  # Process audio every 0.5 seconds
# Observation collection settings
OBSERVATION_WINDOW_DURATION = 5.0  # Time window for observations (seconds)
MAJORITY_THRESHOLD = 0.6  # Minimum percentage for majority (60%)
MAJORITY_CHECK_INTERVAL = 5.0  # Check for majority every 5 seconds
# ----------------

class AudioRecognitionServer:
    def __init__(self):
        self.labels = self.load_labels()
        self.interpreter = self.load_model()

        self.audio_queue = queue.Queue()
        self.is_running = False
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        
        # Audio processing buffers
        buffer_size = int(SAMPLE_RATE * DURATION)
        print(f"Buffer configuration: SAMPLE_RATE={SAMPLE_RATE}, DURATION={DURATION:.3f}s, Buffer size={buffer_size}, Expected input={EXPECTED_INPUT_SIZE}")
        self.audio_buffer = deque(maxlen=buffer_size)
        self.last_process_time = time.time()
        self.last_detection_time = 0
        self.detection_cooldown = 0.5  # Minimum time between detections (half the input length)
        
        # Observation collection for majority voting
        self.observations = deque()
        self.observation_timestamps = deque()
        self.last_majority_check_time = 0
        self.last_majority_send_time = 0
        self.majority_cooldown = 2.0  # Minimum time between majority sends (seconds)
        self.last_sent_majority = None
        
    def load_labels(self):
        """Load classification labels from file"""
        try:
            with open(LABELS_PATH, "r") as f:
                labels = [line.strip().split(maxsplit=1)[1] for line in f.readlines()]
            print(f"Loaded {len(labels)} labels: {labels}")
            return labels
        except Exception as e:
            print(f"Error loading labels: {e}")
            return ["Background Noise", "Cat", "Chicken", "Cow", "Frog", "Mouse", "Seagull"]
    
    def load_model(self):
        """Load TensorFlow Lite model"""
        try:
            interpreter = tf.lite.Interpreter(model_path=MODEL_PATH)
            interpreter.allocate_tensors()
            print(f"Model loaded successfully from {MODEL_PATH}")
            return interpreter
        except Exception as e:
            print(f"Error loading model: {e}")
            return None
    
    def preprocess_audio(self, audio_data):
        """Preprocess audio data for model input"""
        try:
            #print(f"Preprocessing: input length {len(audio_data)}, expected {EXPECTED_INPUT_SIZE}")
            
            # Ensure audio is the right length
            if len(audio_data) < EXPECTED_INPUT_SIZE:
                # Pad with zeros if too short
                #print(f"Padding audio from {len(audio_data)} to {EXPECTED_INPUT_SIZE}")
                audio_data = np.pad(audio_data, (0, EXPECTED_INPUT_SIZE - len(audio_data)))
            elif len(audio_data) > EXPECTED_INPUT_SIZE:
                # Truncate if too long
                #print(f"Truncating audio from {len(audio_data)} to {EXPECTED_INPUT_SIZE}")
                audio_data = audio_data[:EXPECTED_INPUT_SIZE]
            
            #print(f"Length after adjustment: {len(audio_data)}")
            
            # Normalize audio
            audio_data = audio_data.astype(np.float32)
            max_amplitude = np.max(np.abs(audio_data))
            #print(f"Max amplitude before normalization: {max_amplitude}")
            
            if max_amplitude > 0:
                audio_data = audio_data / max_amplitude
                #print(f"Normalized, new max amplitude: {np.max(np.abs(audio_data))}")
            #else:
                #print("Zero amplitude audio, skipping normalization")
            
            # Reshape for model input
            audio_data = audio_data.reshape(1, -1)
            #print(f"Final shape: {audio_data.shape}")
            
            return audio_data
        except Exception as e:
            print(f"Error preprocessing audio: {e}")
            import traceback
            traceback.print_exc()
            return None
    
    def classify_audio(self, audio_data):
        """Run inference on audio data"""
        if self.interpreter is None:
            print("✗ Interpreter is None, cannot classify")
            return None, 0.0
        
        try:
            #print(f"Preprocessing audio: {len(audio_data)} samples")
            # Preprocess audio
            processed_audio = self.preprocess_audio(audio_data)
            if processed_audio is None:
                print("✗ Preprocessing failed")
                return None, 0.0
            
            #print(f"Preprocessed audio shape: {processed_audio.shape}")
            
            # Get input and output details
            input_details = self.interpreter.get_input_details()
            output_details = self.interpreter.get_output_details()
            
            #print(f"Input details: {input_details[0]}")
            #print(f"Output details: {output_details[0]}")
            
            # Set input tensor
            self.interpreter.set_tensor(input_details[0]['index'], processed_audio)
            
            # Run inference
            #print("Running inference...")
            self.interpreter.invoke()
            
            # Get output
            output_data = self.interpreter.get_tensor(output_details[0]['index'])
            probabilities = output_data[0]
            
            #print(f"Raw probabilities: {probabilities}")
            
            # Find best prediction
            best_idx = np.argmax(probabilities)
            confidence = probabilities[best_idx]
            
            #print(f"Best index: {best_idx}, Confidence: {confidence:.3f}")
            
            if confidence >= CONFIDENCE_THRESHOLD:
                predicted_label = self.labels[best_idx] if best_idx < len(self.labels) else f"Unknown_{best_idx}"
                #print(f"✓ High confidence prediction: {predicted_label}")
                return predicted_label, confidence
            else:
                #print(f"✗ Low confidence, returning Background Noise")
                return "Background Noise", confidence
                
        except Exception as e:
            print(f"Error during classification: {e}")
            import traceback
            traceback.print_exc()
            return None, 0.0
    
    def add_observation(self, label, confidence, timestamp):
        """Add a new observation to the collection"""
        # Only add animal classifications, not background noise
        animal_classes = ["Cow", "Mouse", "Frog", "Seagull", "Chicken", "Cat"]
        
        if label in animal_classes:
            self.observations.append(label)
            self.observation_timestamps.append(timestamp)
            print(f"Added observation: {label} (confidence: {confidence:.3f})")
            print(f"Observations in window: {len(self.observations)}")
        else:
            print(f"Skipping background noise: {label}")
    
    def clean_old_observations(self, current_time):
        """Remove observations older than the time window"""
        while self.observation_timestamps and (current_time - self.observation_timestamps[0]) > OBSERVATION_WINDOW_DURATION:
            self.observation_timestamps.popleft()
            self.observations.popleft()
            print("Removed old observation due to time window")
    
    def get_majority_class(self):
        """Determine the majority class from collected observations"""
        if not self.observations:
            return None, 0.0
        
        # Count occurrences of each class
        class_counts = Counter(self.observations)
        total_observations = len(self.observations)
        
        # Find the most common class
        most_common_class, count = class_counts.most_common(1)[0]
        majority_percentage = count / total_observations
        
        print(f"Majority analysis: {class_counts}")
        print(f"Most common: {most_common_class} ({count}/{total_observations} = {majority_percentage:.1%})")
        
        # Check if we have a clear majority
        if majority_percentage >= MAJORITY_THRESHOLD:
            return most_common_class, majority_percentage
        else:
            print(f"No clear majority (need {MAJORITY_THRESHOLD:.1%}, got {majority_percentage:.1%})")
            return None, majority_percentage
    
    def send_majority_via_udp(self, majority_class, majority_percentage):
        """Send the majority class via UDP"""
        current_time = time.time()
        
        # Check cooldown to avoid spam
        if current_time - self.last_majority_send_time < self.majority_cooldown:
            print(f"⏳ Skipping majority send due to cooldown ({self.majority_cooldown - (current_time - self.last_majority_send_time):.1f}s remaining)")
            return
        
        # Don't send if it's the same as last sent
        if majority_class == self.last_sent_majority:
            print(f"⏳ Skipping majority send - same as last sent: {majority_class}")
            return
        
        # Send result via UDP
        message = f"{majority_class},{majority_percentage:.3f}".encode()
        print(f"🎯 Sending MAJORITY via UDP: {message.decode()} to {UDP_IP}:{UDP_PORT}")
        self.socket.sendto(message, (UDP_IP, UDP_PORT))
        
        self.last_majority_send_time = current_time
        self.last_sent_majority = majority_class
        
        # Clear observations after sending
        self.observations.clear()
        self.observation_timestamps.clear()
        print("Cleared observations after sending majority")
    
    def check_majority_periodically(self):
        """Check for majority class every 5 seconds"""
        print("Majority checking thread started")
        while self.is_running:
            try:
                current_time = time.time()
                
                # Check if it's time to check for majority
                if current_time - self.last_majority_check_time >= MAJORITY_CHECK_INTERVAL:
                    print(f"\n⏰ Time to check majority (every {MAJORITY_CHECK_INTERVAL}s)")
                    
                    # Clean old observations first
                    self.clean_old_observations(current_time)
                    
                    # Check if we have any observations
                    if self.observations:
                        majority_class, majority_percentage = self.get_majority_class()
                        if majority_class:
                            self.send_majority_via_udp(majority_class, majority_percentage)
                        else:
                            print("No clear majority found in current window")
                    else:
                        print("No observations in current window")
                    
                    self.last_majority_check_time = current_time
                
                # Sleep for a short time before next check
                time.sleep(0.1)
                
            except Exception as e:
                print(f"Error in majority checking: {e}")
                import traceback
                traceback.print_exc()
    
    def audio_callback(self, indata, frames, time_info, status):
        """Callback for audio input"""
        #print(f"Audio input received: {frames} frames, shape: {indata.shape}, max amplitude: {np.max(np.abs(indata)):.4f}")
        
        if status:
            print(f"Audio callback status: {status}")
        
        # Convert to mono if stereo
        if indata.ndim > 1:
            audio_data = np.mean(indata, axis=1)
        else:
            audio_data = indata.flatten()
        
        # Add to buffer
        self.audio_buffer.extend(audio_data)
        #print(f"Buffer size after adding: {len(self.audio_buffer)}")
        
        # Check if we should process the audio buffer
        current_time = time.time()
        if current_time - self.last_process_time >= PROCESS_INTERVAL:
            #print(f"Time to process! Buffer size: {len(self.audio_buffer)}, Expected: {EXPECTED_INPUT_SIZE}")
            # Get the current buffer content
            if len(self.audio_buffer) >= EXPECTED_INPUT_SIZE:
                # Convert deque to numpy array
                buffer_array = np.array(list(self.audio_buffer))
                # Put in queue for processing
                self.audio_queue.put(buffer_array.copy())
                self.last_process_time = current_time
                #print(f"✓ Added to processing queue: {len(buffer_array)} samples, max amplitude: {np.max(np.abs(buffer_array)):.4f}")
            #else:
                #print(f"✗ Buffer too small: {len(self.audio_buffer)} < {EXPECTED_INPUT_SIZE}")
        else:
            time_until_process = PROCESS_INTERVAL - (current_time - self.last_process_time)
            #print(f"Waiting {time_until_process:.2f}s until next processing")
    
    def process_audio_queue(self):
        """Process audio from queue"""
        print("Processing thread started")
        while self.is_running:
            try:
                # Get audio data from queue with timeout
                audio_data = self.audio_queue.get(timeout=0.1)
                #print(f"✓ Retrieved audio from queue: {len(audio_data)} samples")
                
                # Check cooldown to avoid spam
                current_time = time.time()
                if current_time - self.last_detection_time < self.detection_cooldown:
                    #print(f"⏳ Skipping due to cooldown ({self.detection_cooldown - (current_time - self.last_detection_time):.1f}s remaining)")
                    continue
                
                # Classify audio
                #print("Running classification...")
                label, confidence = self.classify_audio(audio_data)
                #print(f"Classification result: {label} (confidence: {confidence:.3f})")
                
                if label:
                    # Add observation to collection
                    self.add_observation(label, confidence, current_time)
                    
                    # Clean old observations
                    self.clean_old_observations(current_time)
                    
                    self.last_detection_time = current_time
                #else:
                    #print(f"Classification failed")
                
            except queue.Empty:
                # print("Queue empty, waiting...")
                continue
            except Exception as e:
                print(f"Error processing audio: {e}")
                import traceback
                traceback.print_exc()
    
    def start_server(self):
        """Start the audio recognition server"""
        if self.interpreter is None:
            print("Error: Model not loaded. Cannot start server.")
            return
        
        self.is_running = True
        
        # Start audio processing thread
        processing_thread = threading.Thread(target=self.process_audio_queue)
        processing_thread.daemon = True
        processing_thread.start()
        
        # Start majority checking thread
        majority_thread = threading.Thread(target=self.check_majority_periodically)
        majority_thread.daemon = True
        majority_thread.start()
        
        print(f"Starting audio recognition server...")
        print(f"Listening on microphone at {SAMPLE_RATE}Hz")
        print(f"UDP output: {UDP_IP}:{UDP_PORT}")
        print(f"Confidence threshold: {CONFIDENCE_THRESHOLD}")
        print(f"Processing interval: {PROCESS_INTERVAL}s")
        print(f"Detection cooldown: {self.detection_cooldown}s")
        print(f"Observation window duration: {OBSERVATION_WINDOW_DURATION}s")
        print(f"Majority threshold: {MAJORITY_THRESHOLD:.1%}")
        print(f"Majority check interval: {MAJORITY_CHECK_INTERVAL}s")
        print(f"Majority cooldown: {self.majority_cooldown}s")
        print("Press Ctrl+C to stop")
        
        try:
            print("Starting audio stream...")
            # Start audio stream with larger blocksize for better performance
            with sd.InputStream(
                callback=self.audio_callback,
                channels=1,
                samplerate=SAMPLE_RATE,
                blocksize=int(SAMPLE_RATE * 0.1)  # 100ms blocks
            ):
                print("Audio stream started successfully!")
                while self.is_running:
                    time.sleep(0.1)
                    
        except KeyboardInterrupt:
            print("\nStopping server...")
        except Exception as e:
            print(f"Error in audio stream: {e}")
        finally:
            self.stop_server()
    
    def stop_server(self):
        """Stop the audio recognition server"""
        self.is_running = False
        self.socket.close()
        print("Server stopped.")

def main():
    """Main function to run the audio recognition server"""
    server = AudioRecognitionServer()
    server.start_server()

if __name__ == "__main__":
    main()
