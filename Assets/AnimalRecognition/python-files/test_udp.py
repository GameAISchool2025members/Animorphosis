#!/usr/bin/env python3
"""
UDP Connection Test Script

This script tests the UDP connection between the animal recognition system and Unity.
It sends a test message to the Unity AudioListener on port 5005.
"""

import socket
import json
import time
from datetime import datetime

def test_udp_connection(host='127.0.0.1', port=5005):
    """Test UDP connection by sending a test message"""
    try:
        # Create UDP socket
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        
        # Create test message
        test_message = {
            'animal': 'test_animal',
            'confidence': 0.95,
            'timestamp': datetime.now().isoformat(),
            'type': 'test_message'
        }
        
        # Convert to JSON and encode
        message_str = json.dumps(test_message)
        message_bytes = message_str.encode('utf-8')
        
        print(f"Sending test message to {host}:{port}")
        print(f"Message: {message_str}")
        
        # Send message
        sock.sendto(message_bytes, (host, port))
        
        print("✓ Test message sent successfully!")
        print("Check Unity console for received message")
        
        sock.close()
        return True
        
    except Exception as e:
        print(f"✗ Error sending test message: {e}")
        return False

def main():
    """Main function"""
    print("UDP Connection Test")
    print("=" * 30)
    print("This script tests the UDP connection to Unity AudioListener")
    print("Make sure Unity is running with the AudioListener script active")
    print()
    
    # Test the connection
    success = test_udp_connection()
    
    if success:
        print("\n✓ UDP connection test completed successfully!")
        print("If you don't see the message in Unity console, check:")
        print("1. Unity is running with AudioListener script active")
        print("2. No firewall blocking port 5005")
        print("3. Both applications are on the same machine (127.0.0.1)")
    else:
        print("\n✗ UDP connection test failed!")
        print("Check the error message above for details")

if __name__ == "__main__":
    main() 