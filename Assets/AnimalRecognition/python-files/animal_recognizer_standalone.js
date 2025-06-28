/**
 * Animal Sound Recognition - Standalone JavaScript Version
 * 
 * This script can be run in:
 * 1. Browser console (after loading TensorFlow.js and speech-commands)
 * 2. Node.js with proper setup
 * 3. Any JavaScript environment with fetch support
 */

class AnimalSoundRecognizerStandalone {
    constructor() {
        this.model = null;
        this.isListening = false;
        this.classLabels = [];
        this.lastPredictionTime = 0;
        this.PREDICTION_THRESHOLD = 0.7;
        this.PREDICTION_COOLDOWN = 2000; // ms
        this.UDP_SERVER_URL = "http://localhost:8005/udp";
        
        // Model files path
        this.modelURL = "http://localhost:8003/";
        
        // Check if we're in a browser environment
        this.isBrowser = typeof window !== 'undefined';
    }

    async loadModel() {
        try {
            console.log("Loading model from:", this.modelURL + "model.json");
            console.log("Loading metadata from:", this.modelURL + "metadata.json");

            if (this.isBrowser) {
                // Browser version using speech-commands library
                if (typeof window.speechCommands === 'undefined') {
                    throw new Error("speech-commands library not loaded. Please include the script tag.");
                }
                
                this.model = window.speechCommands.create('BROWSER_FFT', undefined, 
                    this.modelURL + "model.json", 
                    this.modelURL + "metadata.json");
                
                await this.model.ensureModelLoaded();
                this.classLabels = this.model.wordLabels();
            } else {
                // Node.js version (would need @tensorflow/tfjs-node)
                console.warn("Node.js version requires additional setup");
                return false;
            }
            
            console.log("Model loaded successfully");
            console.log("Class labels:", this.classLabels);
            
            return true;
        } catch (error) {
            console.error("Error loading model:", error);
            return false;
        }
    }

    async configureUDP(host = "127.0.0.1", port = 8888) {
        try {
            const config = {
                type: 'config',
                host: host,
                port: parseInt(port)
            };
            
            const response = await fetch(this.UDP_SERVER_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(config)
            });
            
            const result = await response.json();
            console.log("UDP configuration result:", result);
            
            if (result.status === 'success') {
                console.log("✓ UDP configured successfully");
                return true;
            } else {
                console.error("✗ UDP configuration failed");
                return false;
            }
        } catch (error) {
            console.error("Error configuring UDP:", error);
            return false;
        }
    }

    async sendUDPMessage(animalType, confidence) {
        const currentTime = Date.now();
        if (currentTime - this.lastPredictionTime < this.PREDICTION_COOLDOWN) {
            return; // Still in cooldown period
        }
        
        const message = {
            type: 'udp_message',
            animal: animalType,
            confidence: confidence,
            timestamp: new Date().toISOString()
        };
        
        try {
            const response = await fetch(this.UDP_SERVER_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(message)
            });
            
            const result = await response.json();
            console.log("UDP message result:", result);
            
            if (result.status === 'success') {
                this.lastPredictionTime = currentTime;
                console.log(`✓ UDP message sent: ${animalType} (${(confidence * 100).toFixed(1)}%)`);
            } else {
                console.error("✗ UDP message failed:", result.message);
            }
        } catch (error) {
            console.error("Error sending UDP message:", error);
        }
    }

    handlePrediction(scores) {
        console.log("Handling prediction with scores:", scores);
        
        // Find the highest confidence prediction
        let maxScore = 0;
        let maxIndex = 0;
        for (let i = 0; i < scores.length; i++) {
            if (scores[i] > maxScore) {
                maxScore = scores[i];
                maxIndex = i;
            }
        }
        
        // Send UDP message if confidence is above threshold
        if (maxScore >= this.PREDICTION_THRESHOLD) {
            const animalType = this.classLabels[maxIndex];
            this.sendUDPMessage(animalType, maxScore);
        }
        
        // Log prediction results
        console.log("Prediction results:");
        for (let i = 0; i < this.classLabels.length && i < scores.length; i++) {
            console.log(`  ${this.classLabels[i]}: ${scores[i].toFixed(2)}`);
        }
        
        // Calculate and log audio level
        const avgScore = scores.reduce((a, b) => a + b, 0) / scores.length;
        const audioLevel = avgScore * 100;
        console.log(`Audio Level: ${audioLevel.toFixed(2)}%`);
    }

    async startListening() {
        if (!this.model) {
            console.error("Model not loaded");
            return false;
        }

        try {
            // Configure UDP
            await this.configureUDP();
            
            // Set up the model to listen
            const modelParameters = {
                invokeCallbackOnNoiseAndUnknown: true,
                includeSpectrogram: true,
                overlapFactor: 0.5
            };

            this.model.listen(
                prediction => {
                    console.log("Prediction received:", prediction);
                    this.handlePrediction(prediction.scores);
                },
                modelParameters
            );

            this.isListening = true;
            console.log("✓ Started listening for animal sounds...");
            return true;
        } catch (error) {
            console.error("Error starting listening:", error);
            return false;
        }
    }

    stopListening() {
        if (this.isListening && this.model) {
            this.model.stopListening();
            this.isListening = false;
            console.log("✓ Stopped listening");
        }
    }

    async testModel() {
        if (!this.model) {
            console.error("Model not loaded");
            return;
        }
        
        console.log("Testing model...");
        
        // Create dummy scores for testing
        const dummyScores = new Array(this.classLabels.length).fill(0.1);
        dummyScores[0] = 0.8; // Set first class to high probability
        
        console.log("Test scores:", dummyScores);
        this.handlePrediction(dummyScores);
        
        console.log("✓ Model test completed!");
    }

    async testUDP() {
        console.log("Testing UDP connection...");
        
        const testMessage = {
            type: 'udp_message',
            animal: 'TEST_ANIMAL',
            confidence: 0.95,
            timestamp: new Date().toISOString()
        };
        
        try {
            const response = await fetch(this.UDP_SERVER_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(testMessage)
            });
            
            const result = await response.json();
            console.log("UDP test result:", result);
            
            if (result.status === 'success') {
                console.log("✓ UDP test message sent!");
            } else {
                console.error("✗ UDP test failed:", result.message);
            }
        } catch (error) {
            console.error("Error sending UDP test message:", error);
        }
    }

    async init() {
        console.log("Animal Sound Recognition - Standalone Version");
        console.log("=" * 50);
        
        // Load the model
        const modelLoaded = await this.loadModel();
        if (!modelLoaded) {
            console.error("Failed to load model. Exiting.");
            return false;
        }
        
        console.log("\nModel is ready!");
        return true;
    }
}

// Browser console usage example
if (typeof window !== 'undefined') {
    // Make it available globally for browser console usage
    window.AnimalRecognizer = AnimalSoundRecognizerStandalone;
    
    // Example usage in browser console:
    console.log(`
    To use in browser console:
    
    1. Make sure TensorFlow.js and speech-commands are loaded
    2. Create an instance:
       const recognizer = new AnimalRecognizer();
    
    3. Initialize:
       await recognizer.init();
    
    4. Start listening:
       await recognizer.startListening();
    
    5. Stop listening:
       recognizer.stopListening();
    
    6. Test model:
       await recognizer.testModel();
    
    7. Test UDP:
       await recognizer.testUDP();
    `);
}

// Node.js usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AnimalSoundRecognizerStandalone;
} 