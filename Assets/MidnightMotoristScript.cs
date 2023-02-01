using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class MidnightMotoristScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;

    private char[] raceOrder = new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'P', 'W' };
    private char[] currentRace = new char[4];
    private int correctCar;
    private bool playingRace;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start()
    {
        raceOrder = raceOrder.Shuffle();
        Debug.LogFormat("[The Midnight Motorist #{0}] Car color order: {1}", moduleId, raceOrder.Join(""));
        GenerateRace();
    }

    void GenerateRace()
    {
        List<char> usedCars = new List<char>();
        while (usedCars.Count != 4)
        {
            int choice = UnityEngine.Random.Range(0, raceOrder.Length);
            while (usedCars.Contains(raceOrder[choice]))
                choice = UnityEngine.Random.Range(0, raceOrder.Length);
            usedCars.Add(raceOrder[choice]);
        }
        for (int i = 0; i < 4; i++)
            currentRace[i] = usedCars[i];
        for (int i = raceOrder.Length - 1; i >= 0; i--)
        {
            for (int j = 0; j < 4; j++)
            {
                if (currentRace[j] == raceOrder[i])
                {
                    correctCar = j;
                    return;
                }
            }
        }
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            int index = Array.IndexOf(buttons, pressed);
            if (index == 0)
            {
                if (!playingRace)
                {
                    playingRace = true;
                    //Start animation business
                }
                else
                {
                    playingRace = false;
                    //Stop animation business
                    GenerateRace();
                }
            }
            else if (index - 1 == correctCar)
            {
                Debug.LogFormat("[The Midnight Motorist #{0}] Car colors from top to bottom: {1}", moduleId, currentRace.Join(""));
                Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is correct", moduleId, currentRace[index - 1]);
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
                Debug.LogFormat("[The Midnight Motorist #{0}] Module solved", moduleId);
            }
            else
            {
                Debug.LogFormat("[The Midnight Motorist #{0}] Car colors from top to bottom: {1}", moduleId, currentRace.Join(""));
                Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is incorrect", moduleId, currentRace[index - 1]);
                GetComponent<KMBombModule>().HandleStrike();
                GenerateRace();
            }
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} something [Does something]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*something\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Debug.Log("Did something");
            yield break;
        }
    }
}