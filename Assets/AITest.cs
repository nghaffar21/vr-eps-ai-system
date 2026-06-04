using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Diagnostics;

[System.Serializable]
public class OllamaRequest
{
    public string model;
    public string prompt;
    public bool stream;
}

[System.Serializable]
public class OllamaResponse
{
    public string response;
}

public class AITest : MonoBehaviour
{

    private Stopwatch totalTimer = new Stopwatch();
    private Stopwatch deepgramTimer = new Stopwatch();
    private Stopwatch aiTimer = new Stopwatch();
    
    [Header("Deepgram Settings")]
    public string apiKey = "73e3ac647667ed80c5047144a72252b0b1f65b70";
    public int recordingDuration = 7;

    private AudioClip recording;
    private bool isRecording = false;

    private string statusMessage = "Press the button to record and transcribe.";
    
    private string aiResponse = "";
    
    void OnGUI()
    {
        UnityEngine.Debug.Log("inside the gui");
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 24;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 20;
        labelStyle.wordWrap = true;
        labelStyle.normal.textColor = Color.black;

        if (!isRecording)
        {
            if (GUI.Button(new Rect(50, 50, 300, 60), "🎤 Start Recording", style))
            {
                StartRecording();
            }
        }
        else
        {
            GUI.Label(new Rect(50, 50, 400, 60),
                $"Recording... ({recordingDuration}s)", labelStyle);
        }

        GUI.Label(new Rect(50, 130, 700, 400), statusMessage, labelStyle);
    }

    void StartRecording()
    {
        totalTimer.Reset();
        totalTimer.Start();
        if (Microphone.devices.Length == 0)
        {
            statusMessage = "❌ No microphone found!";
            UnityEngine.Debug.LogError("No microphone detected");
            return;
        }

        UnityEngine.Debug.Log("Using mic: " + Microphone.devices[0]);

        isRecording = true;
        statusMessage = $"🔴 Recording for {recordingDuration} seconds...";

        recording = Microphone.Start(null, false, recordingDuration, 16000);

        StartCoroutine(WaitForMicAndRecord());
    }

    IEnumerator WaitForMicAndRecord()
    {
        float timeout = 2f;
        float startTime = Time.time;

        while (Microphone.GetPosition(null) <= 0)
        {
            if (Time.time - startTime > timeout)
            {
                statusMessage = "❌ Microphone failed to start!";
                UnityEngine.Debug.LogError("Mic did not start in time");
                yield break;
            }
            yield return null;
        }

        UnityEngine.Debug.Log("Microphone started successfully");

        yield return new WaitForSeconds(recordingDuration);

        Microphone.End(null);
        isRecording = false;

        UnityEngine.Debug.Log("Recording stopped");

        if (recording == null || recording.samples == 0)
        {
            statusMessage = "❌ Recording failed (no data)";
            UnityEngine.Debug.LogError("Recording is empty");
            yield break;
        }

        UnityEngine.Debug.Log("Samples recorded: " + recording.samples);

        statusMessage = "⏳ Sending to Deepgram...";
        yield return StartCoroutine(SendToDeepgram());
    }

    IEnumerator SendToDeepgram()
    {
        deepgramTimer.Reset();
        deepgramTimer.Start();
        byte[] wavData = AudioClipToWav(recording);

        UnityEngine.Debug.Log("WAV byte size: " + wavData.Length);

        if (wavData.Length < 1000)
        {
            statusMessage = "❌ Audio too small (mic likely failed)";
            UnityEngine.Debug.LogError("WAV too small");
            yield break;
        }

        string url = "https://api.deepgram.com/v1/listen?model=nova-2&language=en";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(wavData);
        request.downloadHandler = new DownloadHandlerBuffer();

        string cleanKey = apiKey.Trim();

        request.SetRequestHeader("Authorization", "Token " + cleanKey);
        request.SetRequestHeader("Content-Type", "audio/wav");

        UnityEngine.Debug.Log("Sending request with key: " + cleanKey);

        yield return request.SendWebRequest();
        
        deepgramTimer.Stop();

        UnityEngine.Debug.Log($"Deepgram time: {deepgramTimer.ElapsedMilliseconds} ms");

        UnityEngine.Debug.Log("Response Code: " + request.responseCode);
        UnityEngine.Debug.Log("Raw Response: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string transcript = ParseTranscript(request.downloadHandler.text);
            statusMessage = "✅ Transcript:\n\n" + transcript;

            yield return StartCoroutine(GetAIResponse(transcript));
        }
        else
        {
            statusMessage = "❌ Error: " + request.error + "\n" + request.downloadHandler.text;
        }
    }

    IEnumerator GetAIResponse(string transcript)
    {
        aiTimer.Reset();
        aiTimer.Start();
        statusMessage += "\n\n🤖 Thinking...";

        string prompt =
            "You are an NPC who is a job interviewer. " +
            "The student said: \"" + transcript + "\". " +
            "Respond with ONE SHORT professional acknowledgement. " +
            "Do not ask questions." +
            "Do not respond questions, refer them to the next interview sessions." +
            "Keep it under 15 words." +
            "If the prompt is irrelevant, tell the user to stay in the scope of the interview.";

        OllamaRequest body = new OllamaRequest
        {
            model = "llama3.2:1b",
            prompt = prompt,
            stream = false
        };

        string json = JsonUtility.ToJson(body);

        UnityWebRequest request = new UnityWebRequest(
            "http://localhost:11434/api/generate",
            "POST"
        );

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        
        aiTimer.Stop();

        UnityEngine.Debug.Log($"AI time: {aiTimer.ElapsedMilliseconds} ms");

        if (request.result == UnityWebRequest.Result.Success)
        {
            string raw = request.downloadHandler.text;

            OllamaResponse response =
                JsonUtility.FromJson<OllamaResponse>(raw);

            aiResponse = response.response;

            statusMessage += "\n\n🤖 NPC:\n" + aiResponse;
        }
        else
        {
            statusMessage += "\n\n❌ AI Error: " + request.error;
        }
    }

    string ParseTranscript(string json)
    {
        const string marker = "\"transcript\":\"";
        int start = json.IndexOf(marker);

        if (start < 0)
            return "Could not parse transcript.\n\nRaw: " + json;

        start += marker.Length;
        int end = json.IndexOf("\"", start);

        return json.Substring(start, end - start);
    }

    byte[] AudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
            intData[i] = (short)(samples[i] * 32767f);

        byte[] bytesData = new byte[intData.Length * 2];
        System.Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

        using (var memStream = new System.IO.MemoryStream())
        {
            int sampleRate = clip.frequency;
            int channels = clip.channels;
            int byteRate = sampleRate * channels * 2;

            memStream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            memStream.Write(System.BitConverter.GetBytes(36 + bytesData.Length), 0, 4);
            memStream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            memStream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
            memStream.Write(System.BitConverter.GetBytes(16), 0, 4);
            memStream.Write(System.BitConverter.GetBytes((short)1), 0, 2);
            memStream.Write(System.BitConverter.GetBytes((short)channels), 0, 2);
            memStream.Write(System.BitConverter.GetBytes(sampleRate), 0, 4);
            memStream.Write(System.BitConverter.GetBytes(byteRate), 0, 4);
            memStream.Write(System.BitConverter.GetBytes((short)(channels * 2)), 0, 2);
            memStream.Write(System.BitConverter.GetBytes((short)16), 0, 2);

            memStream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
            memStream.Write(System.BitConverter.GetBytes(bytesData.Length), 0, 4);
            memStream.Write(bytesData, 0, bytesData.Length);

            return memStream.ToArray();
        }
    }
}
