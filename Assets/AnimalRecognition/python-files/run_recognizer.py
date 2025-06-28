#!/usr/bin/env python3
"""
Animal Sound Recognition Runner

This script provides two ways to run the animal sound recognition:
1. Serve the HTML file locally for web browser access
2. Provide a simple command-line interface to run the recognition
"""

import os
import sys
import webbrowser
import http.server
import socketserver
import threading
import time
from pathlib import Path

class CustomHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    """Custom HTTP request handler that can serve files from multiple directories."""
    
    def __init__(self, *args, **kwargs):
        # Set the base directory to AnimalRecognition (parent of python-files)
        script_dir = Path(__file__).parent
        self.base_directory = script_dir.parent
        super().__init__(*args, directory=str(self.base_directory), **kwargs)
    
    def translate_path(self, path):
        """Translate URL path to file system path."""
        # Remove leading slash
        path = path.lstrip('/')
        
        # If the path is empty or just 'index.html', serve the HTML file from python-files
        if not path or path == 'index.html':
            script_dir = Path(__file__).parent
            return str(script_dir / 'index.html')
        
        # For model files and other files in python-files directory, serve from python-files
        script_dir = Path(__file__).parent
        python_files_path = script_dir / path
        if python_files_path.exists():
            return str(python_files_path)
        
        # Otherwise, serve from the base directory (AnimalRecognition)
        return str(self.base_directory / path)

def serve_html_file(port=8000):
    """Serve the HTML file locally using Python's built-in HTTP server."""
    # Check if index.html exists
    script_dir = Path(__file__).parent
    if not (script_dir / "index.html").exists():
        print("Error: index.html not found in the python-files directory")
        return
    
    # Create HTTP server with custom handler
    handler = CustomHTTPRequestHandler
    
    try:
        with socketserver.TCPServer(("", port), handler) as httpd:
            print(f"Server started at http://localhost:{port}")
            print("Opening browser...")
            webbrowser.open(f"http://localhost:{port}")
            print("Press Ctrl+C to stop the server")
            httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nServer stopped.")
    except OSError as e:
        if e.errno == 48:  # Address already in use
            print(f"Port {port} is already in use. Trying port {port + 1}")
            serve_html_file(port + 1)
        else:
            print(f"Error starting server: {e}")

def check_model_files():
    """Check if the required model files exist."""
    # Get the path to the python-files directory (where the model files are located)
    script_dir = Path("/Users/domiceli/AnimalRunner/Assets/AnimalRecognition/python-files")
    model_dir = script_dir  # Model files are in the same directory as this script
    required_files = ["model.json", "metadata.json", "weights.bin"]
    
    missing_files = []
    for file in required_files:
        if not (model_dir / file).exists():
            missing_files.append(file)
    
    if missing_files:
        print("Warning: The following model files are missing:")
        for file in missing_files:
            print(f"  - {model_dir / file}")
        print(f"\nModel directory: {model_dir}")
        print("\nTo use the animal sound recognition:")
        print("1. Export your model from Teachable Machine")
        print("2. Place the model files in the python-files directory")
        print("3. Run this script again")
        return False
    
    print("✓ Model files found")
    print(f"Model directory: {model_dir}")
    return True

def main():
    """Main function to run the animal sound recognition."""
    print("Animal Sound Recognition Runner")
    print("=" * 40)
    
    # Check if model files exist
    if not check_model_files():
        print("\nStarting server anyway (model files may be loaded from elsewhere)...")
    
    print("\nStarting local server...")
    serve_html_file()

if __name__ == "__main__":
    main() 