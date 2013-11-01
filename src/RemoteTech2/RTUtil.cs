﻿using System;
using KSP.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = System.Object;
using Debug = UnityEngine.Debug;

namespace RemoteTech
{
    public static partial class RTUtil
    {
        public static double GameTime { get { return Planetarium.GetUniversalTime(); } }

        public static readonly String[]
            DistanceUnits = { "", "k", "M", "G", "T" },
            ClassDescripts = {  "Short-Planetary (SP)",
                                "Medium-Planetary (MP)",
                                "Long-Planetary (LP)",
                                "Short-Interplanetary (SI)",
                                "Medium-Interplanetary (MI)",
                                "Long-Interplanetary (LI)"};

        private static readonly Regex mDurationRegex =
            new Regex(@"(?:(?<seconds>\d*\.?\d+)\s*s[a-z]*[,\s]*)?" +
                      @"(?:(?<minutes>\d*\.?\d+)\s*m[a-z]*[,\s]*)?" +
                      @"(?:(?<hours>\d*\.?\d+)\s*h[a-z]*[,\s]*)?");

        public static bool TryParseDuration(String duration, out TimeSpan time)
        {
            time = new TimeSpan();
            MatchCollection matches = mDurationRegex.Matches(duration);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups["seconds"].Success)
                    {
                        time += TimeSpan.FromSeconds(Double.Parse(match.Groups["seconds"].Value));
                    }
                    if (match.Groups["minutes"].Success)
                    {
                        time += TimeSpan.FromMinutes(Double.Parse(match.Groups["minutes"].Value));
                    }
                    if (match.Groups["hours"].Success)
                    {
                        time += TimeSpan.FromHours(Double.Parse(match.Groups["hours"].Value));
                    }
                }
                return true;
            }
            else
            {
                double parsedDouble;
                bool result = Double.TryParse(duration, out parsedDouble);
                time = TimeSpan.FromSeconds(result ? parsedDouble : 0);
                return result;
            }
        }

        public static String Truncate(this String targ, int len)
        {
            const String suffix = "...";
            if (targ.Length > len)
            {
                return targ.Substring(0, len - suffix.Length) + suffix;
            }
            else
            {
                return targ;
            }
        }

        public static String FormatDuration(double duration)
        {
            TimeSpan time = TimeSpan.FromSeconds(duration);
            StringBuilder s = new StringBuilder();
            if (time.TotalHours > 1)
            {
                s.Append(Math.Floor(time.TotalHours));
                s.Append("h");
            }
            if (time.Minutes > 1)
            {
                s.Append(time.Minutes);
                s.Append("m");
            }
            if (time.Seconds > 0 || time.Milliseconds > 0)
            {
                s.Append((time.Seconds + time.Milliseconds / 1000.0f).ToString("F2"));
                s.Append("s");
            }
            return s.ToString();
        }

        public static String FormatConsumption(double consumption)
        {
            return consumption.ToString("F2") + "charge/s";
        }

        public static String FormatSI(double value, String unit)
        {
            int i = (int)RTUtil.Clamp(Math.Floor(Math.Log10(value)) / 3,
                0, DistanceUnits.Length - 1);
            value /= Math.Pow(1000, i);
            return value.ToString("F2") + DistanceUnits[i] + unit;
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
        }

        [Conditional("DEBUG")]
        public static void Log(String message)
        {
            Debug.Log("RemoteTech: " + message);
        }

        [Conditional("DEBUG")]
        public static void Log(String message, params Object[] param)
        {
            Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        [Conditional("DEBUG")]
        public static void Log(String message, params UnityEngine.Object[] param)
        {
            Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return "{" +
                   string.Join(",",
                               dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString())
                                         .ToArray()) + "}";
        }

        public static string ToDebugString<T>(this List<T> list)
        {
            return "{" + string.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }

        

        public static String TargetName(Guid guid)
        {
            ISatellite sat;
            if (RTCore.Instance != null && RTCore.Instance.Network != null && RTCore.Instance.Satellites != null)
            {
                if (guid == System.Guid.Empty)
                {
                    return "No Target";
                }
                if (RTCore.Instance.Network.Planets.ContainsKey(guid))
                {
                    return RTCore.Instance.Network.Planets[guid].name;
                }
                if ((sat = RTCore.Instance.Network[guid]) != null)
                {
                    return sat.Name;
                }
            }
            return "Unknown Target";
        }

        public static Guid Guid(this CelestialBody cb)
        {
            char[] name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            RTUtil.Log("RTUtil.Guid({0}) = {1}", cb.bodyName, s.ToString());
            return new Guid(s.ToString());
        }

        public static bool HasValue(this ProtoPartModuleSnapshot ppms, String value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return Boolean.TryParse(value, out result) ? result : false;
        }

        public static bool GetBool(this ProtoPartModuleSnapshot ppms, String value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return Boolean.TryParse(n.GetValue(value) ?? "False", out result) ? result : false;
        }

        public static void Button(Texture2D icon, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(icon, options))
            {
                onClick.Invoke();
            }
        }

        public static void Button(String text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                onClick.Invoke();
            }
        }

        public static void HorizontalSlider(ref float state, float min, float max, params GUILayoutOption[] options)
        {
            state = GUILayout.HorizontalSlider(state, min, max, options);
        }

        public static void GroupButton(int wide, String[] text, ref int group, params GUILayoutOption[] options)
        {
            group = GUILayout.SelectionGrid(group, text, wide, options);
        }

        public static void GroupButton(int wide, String[] text, ref int group, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            int group2;
            if ((group2 = GUILayout.SelectionGrid(group, text, wide, options)) != group)
            {
                group = group2;
                onStateChange.Invoke(group2);
            }
        }

        public static void StateButton(String text, int state, int value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(state == value, text, GUI.skin.button, options)) != (state == value))
            {
                onStateChange.Invoke(result ? value : ~value);
            }
        }

        public static void TextField(ref String text, params GUILayoutOption[] options)
        {
            text = GUILayout.TextField(text, options);
        }

        public static bool ContainsMouse(this Rect window)
        {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }

        public static void LoadImage(out Texture2D texture, String fileName)
        {
            fileName = fileName.Split('.')[0];
            String path = "RemoteTech2/Textures/" + fileName;
            RTUtil.Log("LoadImage({0})", path);
            texture = GameDatabase.Instance.GetTexture(path, false);
            if (texture == null)
            {
                texture = new Texture2D(32, 32);
                texture.SetPixels32(Enumerable.Repeat((Color32) Color.magenta, 32 * 32).ToArray());
                texture.Apply();
            }
        }

        public static IEnumerable<Transform> FindTransformsWithCollider(Transform input)
        {
            if (input.collider != null)
            {
                yield return input;
            }

            foreach (Transform t in input)
            {
                foreach (Transform x in FindTransformsWithCollider(t))
                {
                    yield return x;
                }
            }
        }

        // Thanks Fractal_UK!
        public static bool IsTechUnlocked(string techid)
        {
            if (techid.Equals("None")) return true;
            try
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) return true;
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode[] scenarios = gameconf.GetNodes("SCENARIO");
                foreach (ConfigNode scenario in scenarios)
                {
                    if (scenario.GetValue("name") == "ResearchAndDevelopment")
                    {
                        ConfigNode[] techs = scenario.GetNodes("Tech");
                        foreach (ConfigNode technode in techs)
                        {
                            if (technode.GetValue("id") == techid)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}