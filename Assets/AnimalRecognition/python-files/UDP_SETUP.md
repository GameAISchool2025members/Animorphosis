# UDP Connection Setup Guide

This guide explains how to set up the UDP connection between the animal sound recognition system and Unity.

## Overview

The system uses UDP to send animal recognition results from the Python-based recognition system to Unity. Here's how it works:

1. **Animal Recognition System** (Python) → Sends UDP messages to port 5005
2. **Unity AudioListener** → Listens on port 5005 for animal recognition results

## Port Configuration

- **Unity AudioListener**: Listens on port 5005
- **UDP Server**: Sends to port 5005 (localhost/127.0.0.1)
- **UDP Proxy**: Sends to port 5005 (localhost/127.0.0.1)
- **UDP Listener**: Listens on port 5005 (for testing)

## Setup Instructions

### 1. Start Unity
1. Open your Unity project
2. Make sure the `AudioListener` script is attached to a GameObject in your scene
3. Start the game in Unity

### 2. Start the Animal Recognition System
You have several options:

#### Option A: Run the Web Interface
```bash
cd Assets/AnimalRecognition/python-files
python run_recognizer.py
```
This will start a web server on port 8000. Open your browser to `http://localhost:8000`

#### Option B: Run the UDP Server Directly
```bash
cd Assets/AnimalRecognition/python-files
python udp_server.py
```
This starts an HTTP server on port 8005 that forwards messages to UDP.

#### Option C: Run the WebSocket Proxy
```bash
cd Assets/AnimalRecognition/python-files
python udp_proxy.py
```
This starts a WebSocket server on port 8004 for real-time communication.

### 3. Test the Connection
```bash
cd Assets/AnimalRecognition/python-files
python test_udp.py
```
This will send a test message to Unity. Check the Unity console for the received message.

## Troubleshooting

### Common Issues

#### 1. "Connection refused" or "Address already in use"
- **Cause**: Port 5005 is already in use or blocked
- **Solution**: 
  - Check if Unity is running with the AudioListener script
  - Make sure no other application is using port 5005
  - Try restarting Unity

#### 2. No messages received in Unity
- **Cause**: Port mismatch or firewall blocking
- **Solution**:
  - Verify Unity AudioListener is listening on port 5005
  - Check that the Python scripts are sending to port 5005
  - Ensure both applications are on the same machine (127.0.0.1)

#### 3. JSON parsing errors in Unity
- **Cause**: Message format mismatch
- **Solution**:
  - Check that messages are valid JSON
  - Verify the `AnimalRecognitionResult` class structure matches the sent data

### Debug Steps

1. **Check Unity Console**: Look for AudioListener debug messages
2. **Test UDP Connection**: Run `python test_udp.py`
3. **Check Port Usage**: Use `netstat -an | grep 5005` (macOS/Linux) or `netstat -an | findstr 5005` (Windows)
4. **Verify Firewall**: Ensure port 5005 is not blocked

### Message Format

The UDP messages should be JSON with this structure:
```json
{
  "animal": "cat",
  "confidence": 0.85,
  "timestamp": "2024-01-01T12:00:00.000Z",
  "type": "recognition_result"
}
```

## Integration with Unity

The `AudioListener` script in Unity will:
1. Listen for UDP messages on port 5005
2. Parse the JSON message
3. Call `HandleAnimalRecognition()` with the parsed result
4. Log the results to the Unity console

You can extend the `HandleAnimalRecognition()` method to:
- Change player characters based on recognized animals
- Trigger animations or sound effects
- Update game state
- Send messages to other game objects

## Files Overview

- `udp_server.py` - HTTP server that forwards to UDP
- `udp_proxy.py` - WebSocket server for real-time communication
- `udp_listener.py` - Simple UDP listener for testing
- `test_udp.py` - UDP connection test script
- `AudioListener.cs` - Unity script that receives UDP messages
- `index.html` - Web interface with UDP configuration

## Security Notes

- This setup is for local development only
- UDP messages are not encrypted
- Only use on trusted networks
- Consider implementing authentication for production use 