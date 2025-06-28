#!/usr/bin/env node

/**
 * Animal Sound Recognition - Node.js Version
 * 
 * This script provides UDP communication and testing functionality for the animal sound recognition.
 * The actual model inference is done in the browser, but this Node.js version can:
 * 1. Test UDP communication
 * 2. Simulate animal sound detection
 * 3. Provide a command-line interface for testing
 */

import fetch from 'node-fetch';
import fs from 'fs';
import path from 'path';

class AnimalSoundRecognizerNode {
    constructor() {
        this.lastPredictionTime = 0;
        this.PREDICTION_THRESHOLD = 0.7;
        this.PREDICTION_COOLDOWN = 2000; // ms
        this.UDP_SERVER_URL = "http://localhost:8005/udp";
        
        // Default class labels (these should match your model)
        this.classLabels = ['cat', 'dog', 'bird', 'cow', 'horse', 'sheep', 'pig', 'chicken'];
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
            console.log("Still in cooldown period, skipping message");
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

    async simulateAnimalDetection(animalType = null, confidence = null) {
        if (!animalType) {
            // Randomly select an animal
            animalType = this.classLabels[Math.floor(Math.random() * this.classLabels.length)];
        }
        
        if (!confidence) {
            // Generate a realistic confidence score
            confidence = 0.7 + Math.random() * 0.25; // Between 0.7 and 0.95
        }
        
        console.log(`Simulating detection of: ${animalType} with confidence: ${(confidence * 100).toFixed(1)}%`);
        
        if (confidence >= this.PREDICTION_THRESHOLD) {
            await this.sendUDPMessage(animalType, confidence);
        } else {
            console.log("Confidence below threshold, not sending UDP message");
        }
    }

    async testModel() {
        console.log("Testing model simulation...");
        
        // Test with different animals and confidence levels
        const testCases = [
            { animal: 'cat', confidence: 0.85 },
            { animal: 'dog', confidence: 0.92 },
            { animal: 'bird', confidence: 0.78 },
            { animal: 'cow', confidence: 0.65 }, // Below threshold
        ];
        
        for (const testCase of testCases) {
            console.log(`\n--- Testing ${testCase.animal} ---`);
            await this.simulateAnimalDetection(testCase.animal, testCase.confidence);
            await new Promise(resolve => setTimeout(resolve, 1000)); // Wait 1 second between tests
        }
        
        console.log("\n✓ Model simulation test completed!");
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

    async startSimulation() {
        console.log("Animal Sound Recognition - Node.js Simulation");
        console.log("=" * 50);
        
        // Configure UDP
        await this.configureUDP();
        
        console.log("\nStarting simulation mode...");
        console.log("This will simulate animal sound detection every 3-5 seconds");
        console.log("Press Ctrl+C to stop");
        
        // Start continuous simulation
        const simulate = async () => {
            await this.simulateAnimalDetection();
            
            // Schedule next simulation in 3-5 seconds
            const delay = 3000 + Math.random() * 2000;
            setTimeout(simulate, delay);
        };
        
        simulate();
    }

    async checkServerStatus() {
        console.log("Checking server status...");
        
        try {
            // Check if UDP server is running
            const udpResponse = await fetch(this.UDP_SERVER_URL, {
                method: 'GET'
            });
            
            if (udpResponse.ok) {
                console.log("✓ UDP server is running");
            } else {
                console.log("✗ UDP server is not responding");
            }
        } catch (error) {
            console.log("✗ UDP server is not running");
        }
        
        try {
            // Check if model server is running
            const modelResponse = await fetch("http://localhost:8003/model.json", {
                method: 'GET'
            });
            
            if (modelResponse.ok) {
                console.log("✓ Model server is running");
            } else {
                console.log("✗ Model server is not responding");
            }
        } catch (error) {
            console.log("✗ Model server is not running");
        }
    }
}

// Command line interface
async function main() {
    const recognizer = new AnimalSoundRecognizerNode();
    
    // Parse command line arguments
    const args = process.argv.slice(2);
    const command = args[0];
    
    switch (command) {
        case 'start':
            await recognizer.startSimulation();
            break;
        case 'test-model':
            await recognizer.testModel();
            break;
        case 'test-udp':
            await recognizer.testUDP();
            break;
        case 'configure-udp':
            const host = args[1] || "127.0.0.1";
            const port = args[2] || 8888;
            await recognizer.configureUDP(host, port);
            break;
        case 'simulate':
            const animal = args[1];
            const confidence = args[2] ? parseFloat(args[2]) : null;
            await recognizer.simulateAnimalDetection(animal, confidence);
            break;
        case 'status':
            await recognizer.checkServerStatus();
            break;
        default:
            console.log("Animal Sound Recognition - Node.js Version");
            console.log("Usage:");
            console.log("  node animal_recognizer_node.js start          - Start simulation mode");
            console.log("  node animal_recognizer_node.js test-model     - Test model simulation");
            console.log("  node animal_recognizer_node.js test-udp       - Test UDP connection");
            console.log("  node animal_recognizer_node.js configure-udp [host] [port] - Configure UDP");
            console.log("  node animal_recognizer_node.js simulate [animal] [confidence] - Simulate detection");
            console.log("  node animal_recognizer_node.js status         - Check server status");
            console.log("");
            console.log("Note: This Node.js version simulates animal detection for testing purposes.");
            console.log("For actual audio processing, use the browser version with simple_recognizer.html");
            break;
    }
}

// Run if this file is executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
    main().catch(console.error);
}

export default AnimalSoundRecognizerNode; 