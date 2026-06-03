using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            if (state == "WaitingForCompile")
            {
                SessionState.SetString(StateKey, "EnteringPlayMode");
                EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
            }
            else if (state == "EnteringPlayMode" && EditorApplication.isPlaying)
            {
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += RunTest;
            }
            else if (state == "InPlayMode" && EditorApplication.isPlaying)
            {
                EditorApplication.update += RunTest;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _enemiesKilled = false;
        private static bool _playerKilled = false;
        private static double _lastActionTime = 0;
        private static List<string> _logs = new List<string>();

        private static void RunTest()
        {
            _frameCount++;
            if (_frameCount < 30) return; // Wait more frames

            if (!_setupDone)
            {
                _setupDone = true;
                _lastActionTime = EditorApplication.timeSinceStartup;
                _logs.Add("Setup complete");
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - _lastActionTime;

            if (!_enemiesKilled)
            {
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                _logs.Add("Enemies count: " + enemies.Length);
                if (enemies.Length > 0)
                {
                    foreach (var e in enemies) Object.Destroy(e);
                    _enemiesKilled = true;
                    _lastActionTime = EditorApplication.timeSinceStartup;
                    _logs.Add("Killed all enemies");
                }
                return;
            }

            if (!_playerKilled && elapsed > 1.0)
            {
                GameObject chest = GameObject.Find("Chest");
                if (chest != null) _logs.Add("Chest active: " + chest.activeInHierarchy);
                
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.GetComponent<HealthSystem>().TakeDamage(1000f);
                    _playerKilled = true;
                    _lastActionTime = EditorApplication.timeSinceStartup;
                    _logs.Add("Killed player");
                }
                return;
            }

            if (_playerKilled && elapsed > 1.0)
            {
                GameObject goCanvas = GameObject.Find("GameOverCanvas");
                bool goActive = goCanvas != null && goCanvas.activeInHierarchy;
                _logs.Add("GameOver active: " + goActive);

                string result = goActive ? "SUCCESS" : "FAILURE: Game Over not shown";
                SessionState.SetString(ResultKey, result + " | Logs: " + string.Join(", ", _logs));
                SessionState.SetString(StateKey, "Done");
                EditorApplication.update -= RunTest;
                EditorApplication.isPlaying = false;
            }
        }
    }
}