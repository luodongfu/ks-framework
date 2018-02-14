﻿using UnityEngine;
using System.Collections;
using KS_Events;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public delegate void CommandHandeler(string[] parameters);

public class KS_ConsoleCommands
{
    public KS_ConsoleCommands()
    {
        KS_Console.Instance.OnCommand += OnCommand;
    }

    private void OnCommand(CommandHandeler handler, string[] args)
    {
        handler(args);
    }                            
    
    public void Destroy()
    {
        KS_Console.Instance.OnCommand -= OnCommand;
    }                                                                   
}

public class ConsoleCommands : KS_ConsoleCommands
{
    public ConsoleCommands() : base()
    {
        KS_Console.Instance.RegisterCommand("qqq", exitgame, "Quick close the game", "", false);
        KS_Console.Instance.RegisterCommand("help", help, "Show all commands, add page number after to change page", "$i", false);
        KS_Console.Instance.RegisterCommand("test", test, "Show all commands", "$s $i $f $b", true);
        KS_Console.Instance.RegisterCommand("findcmd", findCmd, "Search commands by string", "$s $i", false);
    }

    void exitgame(string[] args)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void help(string[] args)
    {
        int pageLength = 5;
        Debug.Log("COMMAND HELP RECEIVED");

        int totalPages = Mathf.CeilToInt(
                                (float) KS_Console.Instance.TotalCommands /
                                (float) pageLength
                                );
        int page = 1;
        int start = 0;

        if(args != null)
        {
            page = int.Parse(args[0]);
        }

        if (page > totalPages) page = totalPages;

        start = pageLength * (page - 1);

        int i = 0;

        foreach(KeyValuePair<string, string> sl in KS_Console.Instance.GetAllCommandsAndHelp)
        {
            if (i < start || i >= start + pageLength) { }
            else
            { 
                KS_Console.Instance.WriteToConsole(
                                    sl.Key +
                                    ": " +
                                    sl.Value,
                                    Color.green);
            }

            i++;
        }

        KS_Console.Instance.WriteToConsole(
                                "page " +
                                page +
                                " of " +
                                totalPages,
                                Color.cyan);

    }

    void test(string[] args)
    {
        Debug.Log("Test wroked");
    }

    void findCmd(string[] args)
    {
        int pageLength = 5;
        SortedList<string, string> filtered = new SortedList<string, string>();

        foreach(KeyValuePair<string, string> sl in KS_Console.Instance.GetAllCommandsAndHelp)
        {
            if (sl.Key.Contains(args[0]))
            {
                filtered.Add(sl.Key, sl.Value);
            }
        }

        int totalPages = Mathf.CeilToInt((float)filtered.Count / (float)pageLength);
        int page = 1;
        int start = 0;

        Debug.Log(args.Length);

        if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
        {
            page = int.Parse(args[1]);
        }

        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        start = pageLength * (page - 1);
        int end = ((start + pageLength) > filtered.Count) ? filtered.Count : start + pageLength;

        for(int i = start; i < end; i++)
        {
            KS_Console.Instance.WriteToConsole(
                                filtered.Keys[i] +
                                ": " +
                                filtered.Values[i],
                                Color.green);
        }

        KS_Console.Instance.WriteToConsole(
                            "Page " +
                            page +
                            " of " +
                            totalPages,
                            Color.cyan);
    }
}

public class KS_Console : KS_EventListener {

    private static KS_Console instance;

    public static KS_Console Instance
    {
        get
        {
            if(instance != null)
            {
                return instance;
            }
            else
            {
                return new KS_Console();
            }
        }
    }

    public delegate void OnCommandDelegate(CommandHandeler handler, string[] args);
    public event OnCommandDelegate OnCommand;
    public delegate void OnLogUpdateDelegate(string text);
    public event OnLogUpdateDelegate OnLogUpdate;
    
    public class ConsoleCommand
    {
        public string command { get; private set; }
        public CommandHandeler handler { get; private set; }
        public string help { get; private set; }
        public string[] format { get; private set; }
        public bool requiresAllParams { get; private set; }

        public int numParams { get; private set; }

        public ConsoleCommand(string command, CommandHandeler handler, string help, string paramFormat, bool requiresAllParams = true)
        {
            this.command = command;
            this.handler = handler;
            this.help = help;
            this.requiresAllParams = requiresAllParams;

            if (!string.IsNullOrEmpty(paramFormat))
            {
                string[] formatSplit = paramFormat.Split(' ');
                this.numParams = formatSplit.Length;
                this.format = ParseFormat(formatSplit);
            }
            else
            {
                this.numParams = 0;
                this.format = null;
            }
        }

        private string[] ParseFormat(string[] format)
        {
            string[] ret = new string[numParams];

            for(int i = 0; i < numParams; i++)
            {
                switch (format[i])
                {
                    case "$I":
                    case "$i":
                        ret[i] = "int";
                        break;

                    case "$F":
                    case "$f":
                        ret[i] = "float";
                        break;
                    
                    case "$B":
                    case "$b":
                        ret[i] = "bool";
                        break;

                    default:
                    case "$s":
                        ret[i] = "string";
                        break;
                }
            }

            return ret;
        }
    }

    private int lastUsedIndex = -1;
    private string log = "";

    private List<string> lastUsed = new List<string>();
    private Dictionary<string, ConsoleCommand> commands = new Dictionary<string, ConsoleCommand>();


    // -

    public KS_Console()
    {
        instance = this;
        KS_EventManager.registerListener(this);
    }

    public void RegisterCommand(string command, CommandHandeler handler, string help, string format, bool requiresAllParams = true)
    {
        commands.Add(command, new ConsoleCommand(command, handler, help, format, requiresAllParams));
    }

    public void RegisterCommand(ConsoleCommand command)
    {
        commands.Add(command.command, command);
    }

    public void RunCommandString(string command)
    {
        Debug.Log("Command: " + command);

        command.TrimStart();
        command.TrimEnd();

        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        lastUsed.Add(command);
        lastUsedIndex = -1;
        AppendLog(command, Color.white);

        string[] full = command.Split(' ');

        string[] args = Regex.Split(StringImplode(full, 1), "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

        for (int i = 0; i < args.Length; i++)
            args[i] = args[i].Replace("\"", string.Empty);

        if(args.Length == 1 && string.IsNullOrEmpty(args[0]))
        {
            args = null;
        }

        RunCommand(full[0].ToLower(), args);
    }

    private string StringImplode(string[] s, int start)
    {
        string r = "";

        for (int i = start; i < s.Length; i++)
        {
            r += s[i] + " ";
        }

        return r.TrimEnd();
    }


    private void RunCommand(string command, string[] Args)
    {
        Debug.Log("Got command: " + command);

        ConsoleCommand toRun = null;

        if(!commands.TryGetValue(command, out toRun))
        {
            AppendLog(string.Format("Unknown command '{0}', type 'help' for list.", command), Color.cyan);
        }
        else
        {
            if(toRun.handler == null)
            {
                AppendLog(string.Format("Unable to process command '{0}', handler was null.", command), Color.red);
            }
            else
            {
                if (Args != null)
                {
                    if (toRun.requiresAllParams)
                    {
                        if (toRun.numParams != Args.Length)
                        {
                            AppendLog("Number of parameters does not match the command", Color.red);
                            return;
                        }
                    }

                    if (!CheckCommandArgsFormat(toRun, Args))
                    {
                        return;
                    }
                }

                if (OnCommand != null)
                    OnCommand(toRun.handler, Args);
            }
        }
    }

    private bool CheckCommandArgsFormat(ConsoleCommand command, string[] args)
    {
        int[] errors = new int[command.numParams];
        bool error = false;

        for(int i = 0; i < args.Length; i++)
        {
            switch (command.format[i])
            {
                case "string":
                        
                    break;

                case "int":
                    int j = 0;
                    if (!int.TryParse(args[i], out j ))
                    {
                        errors[i] = 1;
                        error = true;
                    }
                    break;

                case "float":
                    float k = 0f;
                    if(!float.TryParse(args[i], out k))
                    {
                        errors[i] = 1;
                        error = true;
                    }
                    break;

                case "bool":
                    bool l = false;
                    if(!bool.TryParse(args[i], out l))
                    {
                        errors[i] = 1;
                        error = true;
                    }
                    break;
            }
        }

        if (error)
        {
            string errorText = "Parameters ";
            bool first = true;

            for(int i = 0; i < errors.Length; i++)
            {
                if(errors[i] == 1)
                {
                    if (first)
                    {
                        errorText += (i + 1) + "";
                        first = false;
                    }
                    else
                    {
                        errorText += ", " + (i + 1);
                    }
                }
            }

            errorText += " are incorrect values";

            AppendLog(errorText, Color.red);

            return false;
        }
        else
        {
            return true;
        }
    }


    private void AppendLog(string text, Color colour)
    {
        string add = "\n<color=#" + ColorUtility.ToHtmlStringRGBA(colour) + ">" +
                        text + "</color>";
        log += add;

        if (OnLogUpdate != null)
        {
            OnLogUpdate(add);
        }
    }

    public void eventReceived(KS_Event e)
    {
        
    }

    public void WriteToConsole(string text, Color colour)
    {
        AppendLog(text, colour);
    }

    public int TotalCommands
    {
        get
        {
            return commands.Count;
        }
    }

    public SortedList<string, string> GetAllCommandsAndHelp
    {
        get
        {
            SortedList<string, string> temp = new SortedList<string, string>();

            foreach(KeyValuePair<string, ConsoleCommand> kv in commands)
            {
                temp.Add(kv.Key, kv.Value.help);
            }

            return temp;
        }
    }

    public string GetLog
    {
        get
        {
            return log;
        }
    }

    public string PreviousCommand
    {
        get
        {
            lastUsedIndex++;

            if (lastUsedIndex >= lastUsed.Count) lastUsedIndex--;

            return GetUsed(lastUsedIndex);
        }
    }

    public string NextUsedCommand
    {
        get
        {
            if (lastUsedIndex > -1) lastUsedIndex--;

            return GetUsed(lastUsedIndex);
        }
    }

    private string GetUsed(int index)
    {
        if(index < 0)
        {
            return "";
        }
        else
        {
            return lastUsed[index];
        }
    }

}