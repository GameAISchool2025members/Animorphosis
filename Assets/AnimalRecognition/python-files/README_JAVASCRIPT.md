# Running JavaScript Animal Sound Recognition Without HTML

This document explains how to run the animal sound recognition JavaScript code without the complex HTML page. There are several options available:

## Option 1: Browser Console (Easiest)

### Prerequisites
1. Start the model server (serves model files):
   ```bash
   python run_recognizer.py
   ```

2. Start the UDP server (if you want UDP functionality):
   ```bash
   python udp_server.py
   ```

### Steps
1. Open your browser and navigate to `http://localhost:8000`
2. Open the browser's Developer Tools (F12)
3. Go to the Console tab
4. Copy and paste the contents of `animal_recognizer_standalone.js` into the console
5. Run the following commands:

```javascript
// Create an instance
const recognizer = new AnimalSoundRecognizerStandalone();

// Initialize the model
await recognizer.init();

// Start listening for animal sounds
await recognizer.startListening();

// Stop listening
recognizer.stopListening();

// Test the model
await recognizer.testModel();

// Test UDP connection
await recognizer.testUDP();
```

## Option 2: Simple HTML Page

Use the `simple_recognizer.html` file which provides a minimal interface:

1. Start the servers as mentioned above
2. Open `simple_recognizer.html` in your browser
3. Click "Initialize" to load the model
4. Use the buttons to control the recognizer

## Option 3: Node.js Version

### Setup
1. Install Node.js dependencies:
   ```bash
   npm install
   ```

2. Start the servers as mentioned above

### Usage
```bash
# Start the recognizer
node animal_recognizer_node.js start

# Test the model
node animal_recognizer_node.js test-model

# Test UDP connection
node animal_recognizer_node.js test-udp

# Configure UDP settings
node animal_recognizer_node.js configure-udp 127.0.0.1 8888
```

## Option 4: Include in Your Own HTML

You can include the standalone JavaScript in your own HTML file:

```html
<!DOCTYPE html>
<html>
<head>
    <title>My Animal Recognizer</title>
</head>
<body>
    <!-- Load TensorFlow.js and speech-commands -->
    <script src="https://cdn.jsdelivr.net/npm/@tensorflow/tfjs@4.15.0/dist/tf.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/@tensorflow-models/speech-commands@0.5.4/dist/speech-commands.min.js"></script>
    
    <!-- Load our standalone recognizer -->
    <script src="animal_recognizer_standalone.js"></script>
    
    <script>
        // Your custom code here
        async function startMyRecognizer() {
            const recognizer = new AnimalSoundRecognizerStandalone();
            await recognizer.init();
            await recognizer.startListening();
        }
        
        // Start when page loads
        window.addEventListener('load', startMyRecognizer);
    </script>
</body>
</html>
```

## Option 5: Import as Module

If you're using ES6 modules:

```javascript
import AnimalSoundRecognizerStandalone from './animal_recognizer_standalone.js';

const recognizer = new AnimalSoundRecognizerStandalone();
await recognizer.init();
await recognizer.startListening();
```

## File Descriptions

- **`animal_recognizer_standalone.js`**: The main standalone JavaScript class that can run in any JavaScript environment
- **`animal_recognizer_node.js`**: Node.js specific version with command-line interface
- **`simple_recognizer.html`**: Minimal HTML page for testing the standalone JavaScript
- **`package.json`**: Node.js dependencies and scripts
- **`index.html`**: Original complex HTML page with full UI

## Key Features

All versions provide the same core functionality:
- Load TensorFlow.js model for animal sound recognition
- Process audio input and make predictions
- Send UDP messages when animal sounds are detected
- Test model and UDP functionality
- Configure UDP target settings

## Troubleshooting

1. **Model not loading**: Make sure the model server is running on port 8003
2. **UDP not working**: Make sure the UDP server is running on port 8005
3. **Audio not working**: Check browser permissions for microphone access
4. **Node.js errors**: Make sure all dependencies are installed with `npm install`

## Browser Compatibility

The standalone JavaScript works in all modern browsers that support:
- ES6 classes and async/await
- Fetch API
- Web Audio API (for microphone access)
- TensorFlow.js

## Server Requirements

- **Model Server**: Serves model files (model.json, metadata.json, weights.bin)
- **UDP Server**: Handles UDP message forwarding (optional)
- **HTTP Server**: Serves the HTML files (optional for standalone JS) 