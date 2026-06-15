# AI-Enhanced Workplace Communication Prototype for Unity VR

## Overview

This project is a proof-of-concept demonstrating the integration of speech recognition and local large language models (LLMs) into a Unity-based virtual reality application.

The prototype is part of a research project investigating the use of AI to improve workplace communication training for engineering students.

The application records the user's speech, converts it to text using Deepgram, generates a short contextual response using a locally hosted Llama 3.2 3B model through Ollama, and displays the generated response.

The current implementation focuses on generating short conversational acknowledgements ("glue phrases") that make scripted NPC interactions feel more natural.

---

## Architecture

```
User Speech
      │
      ▼
Deepgram Speech-to-Text
      │
      ▼
Transcript
      │
      ▼
Local Ollama Server
(Llama 3.2 3B)
      │
      ▼
Generated Response
      │
      ▼
Unity Application
```

---

## Technologies

- Unity
- C#
- Deepgram API (Speech-to-Text)
- Ollama
- Llama 3.2 3B

---

## Project Structure

```
Assets/
    DeepgramTest.cs
        Speech-to-text prototype.

    AITest.cs
        Speech-to-text followed by local LLM inference.

Packages/

ProjectSettings/
```

---

## Requirements

- Unity
- Windows (tested)
- Microphone
- Internet connection (for Deepgram)
- Ollama installed locally

---

## Installing Ollama

Install Ollama from:

https://ollama.com/

Download the language model:

```bash
ollama pull llama3.2:3b
```

Start the local server:

```bash
ollama serve
```

The server will be available at:

```
http://localhost:11434
```

---

## Deepgram Configuration

Replace the API key inside the Unity script with your own Deepgram API key.

```
public string apiKey = "YOUR_API_KEY";
```

---

## Running the Project

1. Clone the repository.

2. Open the project in Unity.

3. Start the Ollama server:

```bash
ollama serve
```

4. Ensure the Llama model has been downloaded:

```bash
ollama pull llama3.2:3b
```

5. Connect a microphone.

6. Press Play in Unity.

7. Press the recording button.

8. Speak.

9. The application will:
   - record audio
   - transcribe speech using Deepgram
   - send the transcript to the local LLM
   - display the generated response

---

## Current Functionality

- Speech recording
- Speech-to-text transcription
- Local LLM inference
- Short contextual NPC responses

---

## Future Work

- Text-to-speech NPC output
- Facial animation and lip synchronization
- Integration with Meta Quest 3
- Teacher-defined evaluation rubrics
- Automatic assessment of workplace communication skills
- Multi-turn conversational scenarios

---

## License

This project is released under the MIT License.
