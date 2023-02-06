using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;
using Rnd = UnityEngine.Random;
//using Bingus;
//using Pemus;

public class TheMidnightMotoristScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;

    public KMSelectable LeftStick;

    public SpriteRenderer GoalLine;

    public GameObject LeftStickGO;
    bool CanMoveStick = false;
    Vector3 MousePos = new Vector3(-1000, -1000, -1000);
    bool MoveUp = false;
    bool MoveDown = false; //Have these as two separate bools so no potential funny shenanigans happens

    public Sprite[] CarsSpr;
    public SpriteRenderer[] TestCarsRen;
    public Sprite[] TestRoadsSpr;
    public SpriteRenderer[] TestRoadsRen;

    public SpriteRenderer[] SubCarsRen;
    public Sprite[] SubRoadsSpr;
    public SpriteRenderer SubRoadsRen;

    public GameObject TestPhase;
    public GameObject SubPhase;

    private char[] submitOrder = new char[] { 'B', 'P', 'G', 'V', 'W', 'O', 'Y', 'R' };
    private char[] raceOrder = new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'P', 'W' };
    private char[] currentRace = new char[4];
    private int correctCar;
    private int currentSelection;
    private bool playingRace;
    private bool modifyDistribution;
    private bool submissionMode;

    private float RoadDelay = .1f; //This is the delay that is in between each change of road
    private int RoadIndex = 0; //Index of sprite

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    Coroutine RoadGoBrrrrr;
    Coroutine TestGoBrrrrr;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }

        LeftStick.OnInteract += delegate () { StickPress(); return false; };
        LeftStick.OnInteractEnded += delegate () { StickRelease(); };
    }

    void Start()
    {
        raceOrder = raceOrder.Shuffle();
        if (Rnd.Range(0, 2) == 1)
            modifyDistribution = true;
        for (int i = 0; i < raceOrder.Length; i++)
            Debug.LogFormat("[The Midnight Motorist #{0}] The {1} car will always lose to {2}, and will always beat {3}", moduleId, raceOrder[i], GetCarsAhead(raceOrder[i]).ToCharArray().Shuffle().Join(""), GetCarsBefore(raceOrder[i]).ToCharArray().Shuffle().Join(""));
        //Determine correct car
        GenerateRace();
        StartCoroutine(MoveStick(LeftStickGO));
    }

    void GenerateRace()
    {
        List<char> usedCars = new List<char>();
        while (usedCars.Count != 4)
        {
            int choice = Rnd.Range(0, raceOrder.Length);
            while (usedCars.Contains(raceOrder[choice]))
                choice = Rnd.Range(0, raceOrder.Length);
            usedCars.Add(raceOrder[choice]);
        }
        for (int i = 0; i < 4; i++)
            currentRace[i] = usedCars[i];
        for (int i = 0; i < 4; i++)
            TestCarsRen[i].sprite = CarsSpr[Array.IndexOf(raceOrder, currentRace[i])];
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            int index = Array.IndexOf(buttons, pressed);
            if (index == 0)
            {
                if (!submissionMode)
                {
                    if (!playingRace)
                    {
                        playingRace = true;
                        RoadGoBrrrrr = StartCoroutine(ChangeRoad());
                        TestGoBrrrrr = StartCoroutine(ShowTestRace());
                    }
                    else
                    {
                        playingRace = false;
                        StopCoroutine(RoadGoBrrrrr);
                        StopCoroutine(TestGoBrrrrr);
                        RoadDelay = .1f;
                        RoadIndex = 0;
                        TestRoadsRen[0].sprite = TestRoadsSpr[RoadIndex];
                        TestRoadsRen[1].sprite = TestRoadsSpr[RoadIndex];
                        GoalLine.transform.localPosition = new Vector3(-1.25f, 0.458f, -0.252f);
                        TestCarsRen[0].transform.localPosition = new Vector3(0.7f, 0.458f, -0.757f);
                        TestCarsRen[1].transform.localPosition = new Vector3(0.7f, 0.458f, -0.442f);
                        TestCarsRen[2].transform.localPosition = new Vector3(0.7f, 0.458f, -0.127f);
                        TestCarsRen[3].transform.localPosition = new Vector3(0.7f, 0.458f, 0.188f);
                        GenerateRace();
                    }
                }
                else
                {
                    if (currentSelection == correctCar)
                    {
                        Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is correct", moduleId, submitOrder[currentSelection]);
                        moduleSolved = true;
                    }
                    else
                        Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is incorrect", moduleId, submitOrder[currentSelection]);
                    //Run submit mode race and strike/pass at end
                }
            }
            else if (index == 1)
            {
                submissionMode = !submissionMode;
                if (submissionMode)
                {
                    TestPhase.SetActive(false);
                    SubPhase.SetActive(true);
                    //Show selection cursor
                }
                else
                {
                    TestPhase.SetActive(true);
                    SubPhase.SetActive(false);
                    //Hide selection cursor
                }
            }
            else
            {
                Debug.LogFormat("[The Midnight Motorist #{0}] You dared to fiddle with <insert name here>'s controls", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

    string GetCarsAhead(char car)
    {
        string ahead = "";
        int index = Array.IndexOf(raceOrder, car);
        for (int i = 0; i < (modifyDistribution ? 4 : 3); i++)
        {
            index += 1;
            if (index == raceOrder.Length)
                index = 0;
            ahead += raceOrder[index];
        }
        return ahead;
    }

    string GetCarsBefore(char car)
    {
        string before = "";
        int index = Array.IndexOf(raceOrder, car);
        for (int i = 0; i < (modifyDistribution ? 3 : 4); i++)
        {
            index -= 1;
            if (index == -1)
                index = raceOrder.Length - 1;
            before += raceOrder[index];
        }
        return before;
    }

    void StickPress()
    {
        CanMoveStick = true;
    }

    void StickRelease()
    {
        CanMoveStick = false;
    }

    #region Animation

    IEnumerator ChangeRoad()
    {
        while (true)
        {
            if (TestPhase.activeSelf)
            {
                TestRoadsRen[0].sprite = TestRoadsSpr[RoadIndex];
                TestRoadsRen[1].sprite = TestRoadsSpr[RoadIndex];
            }
            else
            {
                SubRoadsRen.sprite = SubRoadsSpr[RoadIndex];
            }

            yield return new WaitForSecondsRealtime(RoadDelay);
            RoadIndex = (RoadIndex + 1) % 3;
        }
    }

    /*void RaceResult (SpriteRenderer First, SpriteRenderer Second, SpriteRenderer Third, SpriteRenderer Last) {
       StartCoroutine(MoveCar(First, 4));
       StartCoroutine(MoveCar(Second, 3));
       StartCoroutine(MoveCar(Third, 2));
       StartCoroutine(MoveCar(Last, 1.5));
    }

    IEnumerator MoveCar (SpriteRenderer Spr, double invSpeed) {
       for (int i = 0; i < 25; i++) {
          Spr.transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(.056f, 0, 0);
          yield return new WaitForSeconds((float) .1 / (float) invSpeed);
       }
    }*/

    IEnumerator ShowTestRace()
    {
        float[] TempSpeeds1 = { Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f) };
        for (int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds((float).1 / 2);
        }
        TempSpeeds1 = new float[] { Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f) };
        for (int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 4; j++)
            {

                TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds((float).1 / 2);
        }
        RoadDelay /= 5;
        TempSpeeds1 = new float[] { Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f) };
        for (int i = 0; i < 30; i++)
        {

            for (int j = 0; j < 4; j++)
            {
                TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds((float).1 / 2);
        }
        yield return new WaitForSeconds(.9f);
        for (int i = 0; i < 25; i++)
        {
            GoalLine.transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(.02f, 0, 0);
            yield return new WaitForSeconds(.01f);
        }
        StopCoroutine(RoadGoBrrrrr);
        yield return new WaitForSeconds(.5f);
        for (int j = 0; j < 4; j++)
        {
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition = new Vector3(1, 0.458f, TestCarsRen[j].transform.localPosition.z);
        }
        TempSpeeds1 = new float[] { Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f) };
        for (int i = 0; i < 30; i++)
        {

            for (int j = 0; j < 4; j++)
            {

                TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds(.03f);
        }
    }

    IEnumerator MoveStick(GameObject Stick)
    {
        while (true)
        {
            //Debug.Log(Stick.transform.localEulerAngles.x);
            if (MoveUp && (Stick.transform.localEulerAngles.x < 30f || Stick.transform.localEulerAngles.x > 329f))
            {
                Stick.transform.Rotate(new Vector3(3f, 0, 0));
            }
            else if (MoveDown && (Stick.transform.localEulerAngles.x > 330 || Stick.transform.localEulerAngles.x < 31))
            {
                Stick.transform.Rotate(new Vector3(-3f, 0, 0));
            }
            else if (!MoveDown && !MoveUp)
            {
                if (Stick.transform.localEulerAngles.x > 0 && Stick.transform.localEulerAngles.x < 36)
                {
                    Stick.transform.localEulerAngles += new Vector3(-3f, 0, 0);
                    //LeftStickGO.transform.Rotate(new Vector3(-3f, 0, 0));
                }
                else if (Stick.transform.localEulerAngles.x > 300)
                {
                    //LeftStickGO.transform.Rotate(new Vector3(3f, 0, 0));
                    Stick.transform.localEulerAngles += new Vector3(3f, 0, 0);
                }
                if (Math.Abs(1 - Stick.transform.localEulerAngles.x) > 0 && Stick.transform.localEulerAngles.x < 1)
                {
                    Stick.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
            }
            yield return new WaitForSeconds(.01f);
        }
    }

    #endregion

    private void Update()
    {
        if (CanMoveStick)
        {
            if (MousePos == new Vector3(-1000, -1000, -1000))
            {
                MousePos = Input.mousePosition;
            }
            if (Input.mousePosition.y > MousePos.y)
            {
                MoveUp = true;
                MoveDown = false;
            }
            else if (Input.mousePosition.y < MousePos.y)
            { //elif necessary so that if player does not move cursor, stick does not go up
                MoveDown = true;
                MoveUp = false;
            }
        }
        if (!CanMoveStick)
        {
            MousePos = new Vector3(-1000, -1000, -1000);
            MoveUp = false;
            MoveDown = false;
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