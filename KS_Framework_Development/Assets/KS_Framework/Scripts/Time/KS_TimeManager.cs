﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KS_Core.IO;
using System;

namespace KS_Core.GameTime
{
    /// <summary>
    /// In game time tracking in 24 hour cycle
    /// </summary>
    public class KS_TimeManager : KS_Behaviour
    {

        private static KS_TimeManager instance;
        /// <summary>
        /// Current active instance of KS_TimeManager
        /// </summary>
        public static KS_TimeManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }
                else
                {
                    instance = new KS_TimeManager();
                    return instance;
                }
            }
        }

        public delegate void TimeUpdate(int h, int m, int s, DayTimeZone zone);
        /// <summary>
        /// On time increased
        /// </summary>
        public event TimeUpdate OnTimeUpdate;

        public enum DayTimeZone
        {
            Dawn,
            Morning,
            Midday,
            afternoon,
            Dusk,
            Midnight
        }

        /// <summary>
        /// Start time of the Dawn time zone
        /// </summary>
        public int dawnStartTime = 6 * 60;
        /// <summary>
        /// Start time of the morning time zone
        /// </summary>
        public int morningStartTime = 8 * 60;
        /// <summary>
        /// Start time of the midday time zone
        /// </summary>
        public int middayStartTime = 11 * 60;
        /// <summary>
        /// Start time of the afternoon time zone
        /// </summary>
        public int afternoonStartTime = 13 * 60;
        /// <summary>
        /// Start time of the dusk time zone
        /// </summary>
        public int duskStartTime = 18 * 60;
        /// <summary>
        /// Start time of the midnight time zone
        /// </summary>
        public int midnightStartTime = 23 * 60;

        private DayTimeZone currentZone = DayTimeZone.afternoon;

        // ---

        /// <summary>
        /// Time to start at awake (In minutes)
        /// </summary>
        [Range(0, 1439)]
        public int startTime = 900;
        /*[Range(1, 31)]
        public int Day = 1;
        [Range(1, 12)]
        public int Month = 1;
        [Range(1900, 2100)]
        public int Year = 2018;*/
        private bool realTime = false;
        private bool paused = false;

        /// <summary>
        /// Ammount of time per in game minute
        /// </summary>
        public float secondsPerMinute = 3f;


        // - Private
        private int minutesPerDay = 1439;
        private float currentMinute = 0;
        private int currentSecond = 0;
        private float secondTimer = 0;


        private bool setTimeOverTime = false;
        private float setTimeOverTime_time = 0f;
        private float setTimeOverTime_value = 0f;
        private int setTimeOverTime_maxValue = 0;
        private int setTimeOverTime_lastRemovedValue = 0;
        private float setTimeOverTime_counter = 0.0f;

        // ---

        protected override void Awake()
        {
            base.Awake();

            instance = this;

            currentMinute = startTime;
            UpdateTime();
            

            KS_SaveLoad.OnSave += OnSave;
            KS_SaveLoad.OnLoad += OnLoad;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            KS_SaveLoad.OnSave += OnSave;
            KS_SaveLoad.OnLoad += OnLoad;
            instance = null;
        }

        protected override void OnLevelLoaded()
        {
            base.OnLevelLoaded();
            UpdateTime();
        }

        private void OnLoad(KS_SaveGame savegame)
        {
            Debug.Log("Time manager on load");
            Dictionary<string, string> fromSave = (Dictionary<string, string>)savegame.SaveData["KS_TIME"];

            currentMinute = int.Parse(fromSave["curMin"]);
            currentSecond = int.Parse(fromSave["curSec"]);
            currentZone = (DayTimeZone)Enum.Parse(typeof(DayTimeZone), fromSave["curZone"]);
            realTime = bool.Parse(fromSave["realTime"]);
            //paused = bool.Parse(fromSave["paused"]);
        }

        private void OnSave(ref Dictionary<string, object> SaveData)
        {
            Dictionary<string, string> toSave = new Dictionary<string, string>();
            toSave.Add("curMin", currentMinute.ToString());
            toSave.Add("curSec", currentSecond.ToString());
            toSave.Add("curZone", currentZone.ToString());
            toSave.Add("realTime", realTime.ToString());
            toSave.Add("paused", paused.ToString());

            SaveData.Add("KS_TIME", toSave);
        }

        protected override void OnGameStateChange(KS_Manager.GameState state)
        {
            if(state == KS_Manager.GameState.Playing)
            {
                paused = false;
            }
            else
            {
                paused = true;
            }
        }

        private void FixedUpdate()
        {
            if (paused) return;

            if (!setTimeOverTime)
            {
                if (!realTime)
                {
                    UpTime();
                }
                else
                {

                }
            }
            else
            {
                if (setTimeOverTime_counter < 1.0f)
                {
                    setTimeOverTime_value = Mathf.Lerp(0, setTimeOverTime_maxValue, setTimeOverTime_counter);
                    //Debug.Log("Val: " + setTimeOverTime_value + " Last Removed value: " + setTimeOverTime_lastRemovedValue);


                    int total = (int)setTimeOverTime_value - setTimeOverTime_lastRemovedValue;
                    setTimeOverTime_lastRemovedValue = (int)setTimeOverTime_value;

                    AddMinutes(total);

                    setTimeOverTime_counter += Time.fixedDeltaTime / setTimeOverTime_time;

                }
                else
                {
                    int total = (int)setTimeOverTime_maxValue - setTimeOverTime_lastRemovedValue;

                    AddMinutes(total);

                    setTimeOverTime = false;
                }
            }
        }

        private void AddMinutes(int minutes)
        {
            currentMinute += minutes;

            if (currentMinute > minutesPerDay)
            {
                currentMinute = 0 + (currentMinute - minutesPerDay);
            }

            CheckZone();

            if (OnTimeUpdate != null)
            {
                int[] time = GetTime();
                OnTimeUpdate(time[0], time[1], time[2], currentZone);
            }
        }

        private void UpTime()
        {
            secondTimer += Time.deltaTime;

            if (secondTimer >= secondsPerMinute)
            {
                secondTimer = 0f;
                currentSecond = 0;

                currentMinute++;

                if (currentMinute > minutesPerDay)
                {
                    currentMinute = 0;
                }

                CheckZone();

                UpdateTime();
            }
            else
            {
                float t = (secondTimer / secondsPerMinute) * 60;

                if ((int)t % 5 == 0 && (int)t != currentSecond && (int) t < 59)
                {
                    currentSecond = (int)t;

                    UpdateTime();
                }
            }
        }

        private void UpdateTime()
        {
            if (OnTimeUpdate != null)
            {
                int[] time = GetTime();
                OnTimeUpdate(time[0], time[1], time[2], currentZone);
            }
        }

        private void CheckZone()
        {
            if (currentMinute >= dawnStartTime && currentMinute < morningStartTime)
            {
                currentZone = DayTimeZone.Dawn;
            }
            else if (currentMinute >= morningStartTime && currentMinute <= middayStartTime)
            {
                currentZone = DayTimeZone.Morning;
            }
            else if (currentMinute >= middayStartTime && currentMinute <= afternoonStartTime)
            {
                currentZone = DayTimeZone.Midday;
            }
            else if (currentMinute >= afternoonStartTime && currentMinute <= duskStartTime)
            {
                currentZone = DayTimeZone.afternoon;
            }
            else if (currentMinute >= duskStartTime && currentMinute <= midnightStartTime)
            {
                currentZone = DayTimeZone.Dusk;
            }
            else
            {
                currentZone = DayTimeZone.Midnight;
            }
        }

        /// <summary>
        /// Get the current time
        /// </summary>
        /// <returns>Int array: 0 = Hours, 1 = Minutes, 2 = Seconds</returns>
        public int[] GetTime()
        {
            int h = (int)currentMinute / 60;
            int m = (int)currentMinute % 60;

            return new int[] { h, m, currentSecond };
        }

        /// <summary>
        /// Get the current time as a formatted string
        /// </summary>
        /// <returns>formatted string: HH:MM:SS</returns>
        public string GetTimeFormatted()
        {
            int[] t = GetTime();

            string H = "";
            string M = "";

            if (t[0] < 10)
            {
                H = "0";
            }

            if (t[1] < 10)
            {
                M = "0";
            }

            H += t[0].ToString();
            M += t[1].ToString();

            return H + ":" + M + ":" + t[2].ToString();
        }

        /// <summary>
        /// Get the current time of the day as a percentage 0.0 - 1.0
        /// </summary>
        public float TimeOfDay // game time 0 .. 1
        {
            get
            {
                float maxM = minutesPerDay * 60;
                float m = (currentMinute * 60) + (currentSecond / 60);
                return m / maxM;
            }
        }

        /// <summary>
        /// Set the time
        /// </summary>
        /// <param name="hours">Hour 0-23</param>
        /// <param name="minuets">Minute 0-59</param>
        public void SetTime(int hours, int minuets)
        {
            if (hours > 23) hours = 23;
            if (minuets > 59) minuets = 59;

            currentMinute = (hours * 60) + minuets;
        }

        /// <summary>
        /// Set the time scale
        /// </summary>
        /// <param name="scale">Scale as the time per in game minute</param>
        public void SetTimeScale(float scale)
        {
            if (scale < 0.01) scale = 0.01f;
            if (scale > 59.99) scale = 60f;

            secondsPerMinute = scale;
        }

        /// <summary>
        /// Smoothly set the time over inputting time
        /// </summary>
        /// <param name="h">Target hour 0-23</param>
        /// <param name="m">Target second 0-59</param>
        /// <param name="time">Ammount of time to change to target time</param>
        public void SetTimeOverTime(int h, int m, int time)
        {
            // Target Time in minutes
            int total = (h * 60) + m;

            // 
            int minsToAdd = 0;

            if (total > (int)currentMinute)
            {
                minsToAdd = (total - (int)currentMinute);
            }
            else
            {
                int leftToday = minutesPerDay - (int)currentMinute;

                minsToAdd = leftToday + total;
            }

            Debug.Log("Mins to add: " + minsToAdd + " over: " + time + " Seconds");

            setTimeOverTime_time = time;
            setTimeOverTime_value = 0;
            setTimeOverTime_maxValue = minsToAdd;
            setTimeOverTime_lastRemovedValue = 0;
            setTimeOverTime_counter = 0;

            setTimeOverTime = true;
        }


    }
}