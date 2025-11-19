using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Unity Editor Window for viewing Quest debug logs in real-time
/// Window > Quest Debug Console Viewer
/// </summary>
public class QuestLogViewerWindow : EditorWindow
{
    private UdpClient udpClient;
    private bool isListening = false;
    private int udpPort = 12345;
    
    private Vector2 scrollPosition;
    private List<LogEntry> logs = new List<LogEntry>();
    private ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>(); // Thread-safe queue
    private int maxLogs = 1000;
    private int lastLogCount = 0; // Track when new logs are added
    private bool userScrolledUp = false; // Track if user manually scrolled up
    
    private bool autoScroll = true;
    private bool showLogs = true;
    private bool showWarnings = true;
    private bool showErrors = true;
    
    private GUIStyle logStyle;
    private GUIStyle warningStyle;
    private GUIStyle errorStyle;
    private GUIStyle headerStyle;
    
    private Thread receiveThread;
    private bool shouldStopThread = false;
    
    private class LogEntry
    {
        public string type;
        public string timestamp;
        public string message;
        public float timeReceived;
        
        public LogEntry(string type, string timestamp, string message)
        {
            this.type = type;
            this.timestamp = timestamp;
            this.message = message;
            this.timeReceived = Time.realtimeSinceStartup;
        }
    }
    
    [MenuItem("Window/Quest Debug Console Viewer")]
    public static void ShowWindow()
    {
        QuestLogViewerWindow window = GetWindow<QuestLogViewerWindow>("Quest Log Viewer");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }
    
    void OnEnable()
    {
        // Don't initialize styles here - EditorStyles might not be ready yet
        // Styles will be initialized lazily in OnGUI when needed
    }
    
    void OnDisable()
    {
        StopListening();
    }
    
    void InitializeStyles()
    {
        // Lazy initialization - only create styles if they don't exist yet
        if (logStyle == null && EditorStyles.label != null)
        {
            logStyle = new GUIStyle(EditorStyles.label);
            logStyle.wordWrap = true;
            logStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);
        }
        
        if (warningStyle == null && EditorStyles.label != null)
        {
            warningStyle = new GUIStyle(EditorStyles.label);
            warningStyle.wordWrap = true;
            warningStyle.normal.textColor = Color.yellow;
            warningStyle.fontStyle = FontStyle.Bold;
        }
        
        if (errorStyle == null && EditorStyles.label != null)
        {
            errorStyle = new GUIStyle(EditorStyles.label);
            errorStyle.wordWrap = true;
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontStyle = FontStyle.Bold;
        }
        
        if (headerStyle == null && EditorStyles.boldLabel != null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
        }
    }
    
    void OnGUI()
    {
        // Initialize styles if needed (lazy initialization)
        InitializeStyles();
        
        // Header
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Quest Debug Console Viewer", headerStyle != null ? headerStyle : EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Controls
        EditorGUILayout.BeginHorizontal();
        
        // Port input
        EditorGUILayout.LabelField("UDP Port:", GUILayout.Width(70));
        udpPort = EditorGUILayout.IntField(udpPort, GUILayout.Width(80));
        
        GUILayout.FlexibleSpace();
        
        // Start/Stop button
        if (!isListening)
        {
            if (GUILayout.Button("Start Listening", GUILayout.Width(120)))
            {
                StartListening();
            }
        }
        else
        {
            GUI.color = Color.red;
            if (GUILayout.Button("Stop Listening", GUILayout.Width(120)))
            {
                StopListening();
            }
            GUI.color = Color.white;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Status
        EditorGUILayout.BeginHorizontal();
        if (isListening)
        {
            GUI.color = Color.green;
            GUILayout.Label("● Listening", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.gray;
            GUILayout.Label("○ Stopped", EditorStyles.label);
            GUI.color = Color.white;
        }
        
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Logs: {logs.Count}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Filters
        EditorGUILayout.BeginHorizontal();
        showLogs = EditorGUILayout.Toggle("Logs", showLogs, GUILayout.Width(60));
        showWarnings = EditorGUILayout.Toggle("Warnings", showWarnings, GUILayout.Width(80));
        showErrors = EditorGUILayout.Toggle("Errors", showErrors, GUILayout.Width(60));
        
        GUILayout.FlexibleSpace();
        
        autoScroll = EditorGUILayout.Toggle("Auto Scroll", autoScroll, GUILayout.Width(80));
        
        if (GUILayout.Button("Scroll to Bottom", GUILayout.Width(120)))
        {
            scrollPosition.y = Mathf.Infinity;
            userScrolledUp = false; // Reset manual scroll flag
        }
        
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            logs.Clear();
            lastLogCount = 0;
            userScrolledUp = false;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Log display
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Calculate visible log count (after filters)
        int visibleLogCount = 0;
        foreach (var log in logs)
        {
            if (log.type == "LOG" && !showLogs) continue;
            if (log.type == "WARNING" && !showWarnings) continue;
            if (log.type == "ERROR" && !showErrors) continue;
            visibleLogCount++;
        }
        
        // Check if new logs were added
        bool newLogsAdded = visibleLogCount > lastLogCount;
        if (newLogsAdded)
        {
            lastLogCount = visibleLogCount;
        }
        
        // Handle scroll wheel events to detect manual scrolling
        if (Event.current.type == EventType.ScrollWheel)
        {
            float scrollDelta = Event.current.delta.y;
            // If user scrolls up (positive delta means scrolling up in Unity), mark that they're manually scrolling
            if (scrollDelta > 0)
            {
                userScrolledUp = true;
            }
            // If user scrolls down, check if they're near bottom to re-enable auto-scroll
            else if (scrollDelta < 0)
            {
                // We'll check if near bottom after scroll view is updated
                // For now, just allow the scroll to happen
            }
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        
        if (logs.Count == 0)
        {
            EditorGUILayout.HelpBox("No logs received yet. Make sure:\n1. Quest app is running\n2. Network Logging is enabled in QuestDebugConsole\n3. PC IP address is correct in QuestDebugConsole", MessageType.Info);
        }
        else
        {
            foreach (var log in logs)
            {
                // Apply filters
                if (log.type == "LOG" && !showLogs) continue;
                if (log.type == "WARNING" && !showWarnings) continue;
                if (log.type == "ERROR" && !showErrors) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                // Timestamp
                EditorGUILayout.LabelField($"[{log.timestamp}]", EditorStyles.miniLabel, GUILayout.Width(100));
                
                // Type badge - use fallback styles if not initialized
                GUIStyle typeStyle;
                if (log.type == "ERROR")
                    typeStyle = errorStyle != null ? errorStyle : EditorStyles.label;
                else if (log.type == "WARNING")
                    typeStyle = warningStyle != null ? warningStyle : EditorStyles.label;
                else
                    typeStyle = logStyle != null ? logStyle : EditorStyles.label;
                
                EditorGUILayout.LabelField($"[{log.type}]", typeStyle, GUILayout.Width(70));
                
                // Message
                EditorGUILayout.LabelField(log.message, typeStyle);
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(2);
            }
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        
        // Auto-scroll to bottom only when:
        // 1. Auto-scroll is enabled
        // 2. New logs were added
        // 3. User hasn't manually scrolled up
        // 4. On repaint event
        if (autoScroll && newLogsAdded && !userScrolledUp && Event.current.type == EventType.Repaint)
        {
            scrollPosition.y = Mathf.Infinity;
        }
        
        // Reset userScrolledUp flag when user manually scrolls back to bottom
        // Check if scroll position is near the maximum (at bottom)
        if (userScrolledUp && Event.current.type == EventType.ScrollWheel && Event.current.delta.y < 0)
        {
            // User is scrolling down - check if they reach bottom
            // We'll check this after the scroll happens, so we use a small threshold
            if (scrollPosition.y > 1000000) // Very large number means near bottom
            {
                userScrolledUp = false;
            }
        }
        
        // Also reset if user clicks the "Auto Scroll" toggle
        if (!autoScroll)
        {
            userScrolledUp = false; // Reset when auto-scroll is disabled
        }
    }
    
    void StartListening()
    {
        if (isListening) return;
        
        try
        {
            udpClient = new UdpClient(udpPort);
            udpClient.EnableBroadcast = true;
            
            isListening = true;
            shouldStopThread = false;
            
            // Start receive thread
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            Debug.Log($"[QuestLogViewer] Started listening on port {udpPort}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to start listening:\n{e.Message}", "OK");
            isListening = false;
        }
    }
    
    void StopListening()
    {
        if (!isListening) return;
        
        shouldStopThread = true;
        isListening = false;
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000); // Wait up to 1 second
        }
        
        if (udpClient != null)
        {
            try
            {
                udpClient.Close();
            }
            catch { }
            udpClient = null;
        }
        
        Debug.Log("[QuestLogViewer] Stopped listening");
    }
    
    void ReceiveLoop()
    {
        while (!shouldStopThread && udpClient != null)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                
                // Store message in thread-safe queue (will be processed on main thread)
                string message = Encoding.UTF8.GetString(data);
                receivedMessages.Enqueue(message);
            }
            catch (System.Exception e)
            {
                if (!shouldStopThread)
                {
                    // Can't use Debug.Log from background thread, so we'll queue it
                    receivedMessages.Enqueue($"[SYSTEM]|{System.DateTime.Now:HH:mm:ss.fff}|Receive error: {e.Message}");
                }
                Thread.Sleep(100); // Small delay on error
            }
        }
    }
    
    void ParseAndAddLog(string data)
    {
        try
        {
            // Format: [TYPE]|timestamp|message
            string[] parts = data.Split(new char[] { '|' }, 3);
            
            if (parts.Length >= 3)
            {
                string logType = parts[0].Trim('[', ']');
                string timestamp = parts[1];
                string message = parts[2];
                
                logs.Add(new LogEntry(logType, timestamp, message));
                
                // Limit log count
                while (logs.Count > maxLogs)
                {
                    logs.RemoveAt(0);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QuestLogViewer] Parse error: {e.Message}");
        }
    }
    
    void Update()
    {
        // Process received messages from background thread on main thread
        if (isListening)
        {
            // Process all queued messages
            while (receivedMessages.TryDequeue(out string message))
            {
                ParseAndAddLog(message);
            }
            
            // Repaint window if we have new logs
            if (receivedMessages.Count == 0 && logs.Count > 0)
            {
                Repaint();
            }
        }
    }
}

