using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using Rnd = UnityEngine.Random;
//using Bingus;
//using Pemus;

public class TheMidnightMotoristScript : MonoBehaviour
{

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;

    public AudioSource adjustableAudio;
    public AudioClip[] adjustableClips;

    public KMSelectable LeftStick;
    public KMSelectable RightStick;

    public SpriteRenderer GoalLine;

    public Sprite AOneByOnePixelBlackSquare;

    public Light[] Lights;

    public TextMesh volumeText;
    public TextMesh muteText;

    public TextMesh SolveText;
    public TextMesh StrikeText;

    public GameObject Selector;
    public GameObject LeftStickGO;
    public GameObject RightStickGO;
    bool CanMoveLeftStick = false;
    bool CanMoveRightStick = false;
    Vector3 MouseLeftPos = new Vector3(-1000, -1000, -1000);

    bool MoveLeftUp = false;
    bool MoveLeftDown = false; //Have these as two separate bools so no potential funny shenanigans happens

    bool MoveLeftRegister = false;
    bool MaxLeftJoystickDistance = false;

    Vector3 MouseRightPos = new Vector3(-1000, -1000, -1000);

    bool MoveRightUp = false;
    bool MoveRightDown = false; //Have these as two separate bools so no potential funny shenanigans happens

    bool MoveRightRegister = false;
    bool MaxRightJoystickDistance = false;

    public GameObject[] Speakers;

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
    private char[] carColors = new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'P', 'W' };
    private char[] raceOrder = new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'P', 'W' };
    private char[] currentRace = new char[4];
    private int correctCar;
    private int currentSelection;
    private bool playedRace;
    private bool submissionMode;
    private bool animatingRace;

    private float RoadDelay = .1f; //This is the delay that is in between each change of road
    private int RoadIndex = 0; //Index of sprite

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    Coroutine RoadGoBrrrrr;
    Coroutine TestGoBrrrrr;
    Coroutine TickGoBrrrrr;
    Coroutine TickGoBrrrrr2;
    Coroutine MuteCo;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }

        LeftStick.OnInteract += delegate () { StickLeftPress(); return false; };
        LeftStick.OnInteractEnded += delegate () { StickLeftRelease(); };
        RightStick.OnInteract += delegate () { StickRightPress(); return false; };
        RightStick.OnInteractEnded += delegate () { StickRightRelease(); };
    }

    void Start()
    {
        float scalar = transform.lossyScale.x;
        for (var i = 0; i < Lights.Length; i++)
            Lights[i].range *= scalar;
        raceOrder = raceOrder.Shuffle();
        for (int i = 0; i < raceOrder.Length; i++)
            Debug.LogFormat("[The Midnight Motorist #{0}] The {1} car will always lose to {2}, and will always beat {3}", moduleId, raceOrder[i], GetCarsAhead(raceOrder[i]).ToCharArray().Shuffle().Join(""), GetCarsBefore(raceOrder[i]).ToCharArray().Shuffle().Join(""));
        GenerateSubmission(false);
        GenerateRace();
        StartCoroutine(MoveLeftStick());
        StartCoroutine(MoveRightStick());
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
            TestCarsRen[i].sprite = CarsSpr[Array.IndexOf(carColors, currentRace[i])];
    }

    void GenerateSubmission(bool notFirst)
    {
        submitOrder = submitOrder.Shuffle();
        for (int i = 0; i < 8; i++)
            SubCarsRen[i].sprite = CarsSpr[Array.IndexOf(carColors, submitOrder[i])];
        Debug.LogFormat("[The Midnight Motorist #{0}] The cars in the submit phase from top to bottom are{2}: {1}", moduleId, submitOrder.Join(""), notFirst ? " now" : "");
        int bracket1 = 1;
        int bracket2 = 3;
        int bracket3 = 5;
        int bracket4 = 7;
        int bracket5;
        int bracket6;
        if (GetCarsBefore(submitOrder[0]).Contains(submitOrder[1]))
            bracket1 = 0;
        if (GetCarsBefore(submitOrder[2]).Contains(submitOrder[3]))
            bracket2 = 2;
        if (GetCarsBefore(submitOrder[4]).Contains(submitOrder[5]))
            bracket3 = 4;
        if (GetCarsBefore(submitOrder[6]).Contains(submitOrder[7]))
            bracket4 = 6;
        if (GetCarsBefore(submitOrder[bracket1]).Contains(submitOrder[bracket2]))
            bracket5 = bracket1;
        else
            bracket5 = bracket2;
        if (GetCarsBefore(submitOrder[bracket3]).Contains(submitOrder[bracket4]))
            bracket6 = bracket3;
        else
            bracket6 = bracket4;
        if (GetCarsBefore(submitOrder[bracket5]).Contains(submitOrder[bracket6]))
            correctCar = bracket5;
        else
            correctCar = bracket6;
        Debug.LogFormat("[The Midnight Motorist #{0}] The correct car to select is: {1}", moduleId, submitOrder[correctCar]);
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            int index = Array.IndexOf(buttons, pressed);
            if (index == 0)
            {
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                if (!submissionMode)
                {
                    if (!playedRace)
                    {
                        playedRace = true;
                        animatingRace = true;
                        TestGoBrrrrr = StartCoroutine(ShowTestRace());
                    }
                    else
                    {
                        playedRace = false;
                        animatingRace = false;
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
                else if (!animatingRace)
                {
                    animatingRace = true;
                    if (currentSelection == correctCar)
                    {
                        Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is correct", moduleId, submitOrder[currentSelection]);
                        StartCoroutine(ShowFinalRace(true));
                    }
                    else
                    {
                        Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is incorrect", moduleId, submitOrder[currentSelection]);
                        StartCoroutine(ShowFinalRace(false));
                    }
                }
            }
            else if (index == 1)
            {
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                if (!animatingRace)
                {
                    submissionMode = !submissionMode;
                    if (submissionMode)
                    {
                        RoadDelay = .1f;
                        RoadIndex = 0;
                        GoalLine.transform.localPosition = new Vector3(-1.25f, 0.458f, -0.252f);
                        TestPhase.SetActive(false);
                        SubPhase.SetActive(true);
                    }
                    else
                    {
                        if (playedRace)
                            GoalLine.transform.localPosition = new Vector3(-0.77f, 0.458f, -0.252f);
                        TestPhase.SetActive(true);
                        SubPhase.SetActive(false);
                    }
                }
            }
            else if (index == 2)
            {
                audio.PlaySoundAtTransform("ButtonFail", transform);
            }
            else if (index == 3)
            {
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                if (adjustableAudio.mute == false)
                {
                    MuteCo = StartCoroutine(MuteTick());
                    adjustableAudio.mute = true;
                }
                else
                {
                    StopCoroutine(MuteCo);
                    muteText.text = "";
                    adjustableAudio.mute = false;
                }
            }
        }
    }

    string GetCarsAhead(char car)
    {
        string ahead = "";
        int index = Array.IndexOf(raceOrder, car);
        for (int i = 0; i < (Array.IndexOf(raceOrder, car) < 4 ? 3 : 4); i++)
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
        for (int i = 0; i < (Array.IndexOf(raceOrder, car) < 4 ? 4 : 3); i++)
        {
            index -= 1;
            if (index == -1)
                index = raceOrder.Length - 1;
            before += raceOrder[index];
        }
        return before;
    }

    void StickLeftPress()
    {
        CanMoveLeftStick = true;
    }

    void StickLeftRelease()
    {
        CanMoveLeftStick = false;
        if (TickGoBrrrrr != null)
        {
            StopCoroutine(TickGoBrrrrr);
        }
    }

    void StickRightPress()
    {
        CanMoveRightStick = true;
        if (moduleSolved) return;
        volumeText.text = "VOL: " + Math.Round(adjustableAudio.volume * 100, 1) + "%";
    }

    void StickRightRelease()
    {
        CanMoveRightStick = false;
        if (TickGoBrrrrr2 != null)
        {
            StopCoroutine(TickGoBrrrrr2);
        }
        volumeText.text = "";
    }

    bool ValueTooClose(List<float> speeds, float speed, float threshold)
    {
        for (int i = 0; i < speeds.Count; i++)
        {
            if (((speeds[i] + threshold) > speed) && ((speeds[i] - threshold) < speed))
                return true;
        }
        return false;
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
        adjustableAudio.clip = adjustableClips[1];
        for (int i = 0; i < 3; i++)
        {
            adjustableAudio.Play();
            yield return new WaitForSeconds(.4f);
        }
        adjustableAudio.clip = adjustableClips[2];
        adjustableAudio.Play();
        RoadGoBrrrrr = StartCoroutine(ChangeRoad());
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
        for (int i = 0; i < 12; i++)
        {
            GoalLine.transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(.04f, 0, 0);
            yield return new WaitForSeconds(.01f);
        }
        StopCoroutine(RoadGoBrrrrr);
        yield return new WaitForSeconds(.5f);
        for (int j = 0; j < 4; j++)
        {
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition = new Vector3(1, 0.458f, TestCarsRen[j].transform.localPosition.z);
        }
        TempSpeeds1 = new float[4];
        List<float> genSpeeds = new List<float>();
        for (int i = 0; i < 4; i++)
        {
            float gen = Rnd.Range(0.12f, 0.22f);
            while (ValueTooClose(genSpeeds, gen, .01f))
                gen = Rnd.Range(0.12f, 0.22f);
            genSpeeds.Add(gen);
        }
        genSpeeds.Sort();
        int bracket1;
        int bracket2;
        int loser1;
        int loser2;
        if (GetCarsAhead(currentRace[0]).Contains(currentRace[1]))
        {
            bracket1 = 1;
            loser1 = 0;
        }
        else
        {
            bracket1 = 0;
            loser1 = 1;
        }
        if (GetCarsAhead(currentRace[2]).Contains(currentRace[3]))
        {
            bracket2 = 3;
            loser2 = 2;
        }
        else
        {
            bracket2 = 2;
            loser2 = 3;
        }
        if (GetCarsAhead(currentRace[bracket1]).Contains(currentRace[bracket2]))
        {
            TempSpeeds1[bracket2] = genSpeeds[3];
            TempSpeeds1[bracket1] = genSpeeds[2];
        }
        else
        {
            TempSpeeds1[bracket1] = genSpeeds[3];
            TempSpeeds1[bracket2] = genSpeeds[2];
        }
        if (GetCarsAhead(currentRace[loser1]).Contains(currentRace[loser2]))
        {
            TempSpeeds1[loser2] = genSpeeds[1];
            TempSpeeds1[loser1] = genSpeeds[0];
        }
        else
        {
            TempSpeeds1[loser1] = genSpeeds[1];
            TempSpeeds1[loser2] = genSpeeds[0];
        }
        for (int i = 0; i < 30; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds(.03f);
        }
        animatingRace = false;
    }

    IEnumerator ShowFinalRace(bool correct)
    {
        adjustableAudio.clip = adjustableClips[0];
        adjustableAudio.Play();
        Selector.SetActive(false);
        yield return new WaitForSeconds(1f);
        RoadGoBrrrrr = StartCoroutine(ChangeRoad());
        float[] TempSpeeds1 = { Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f) };
        for (int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds((float).1 / 2);
        }
        TempSpeeds1 = new float[] { Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f) };
        for (int i = 0; i < 30; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds((float).1 / 2);
        }
        RoadDelay /= 5;
        TempSpeeds1 = new float[] { Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f) };
        for (int i = 0; i < 30; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds((float).1 / 2);
        }
        yield return new WaitForSeconds(.4f);
        for (int i = 0; i < 12; i++)
        {
            GoalLine.transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(.04f, 0, 0);
            yield return new WaitForSeconds(.01f);
        }
        StopCoroutine(RoadGoBrrrrr);
        yield return new WaitForSeconds(1.8f);
        for (int j = 0; j < 8; j++)
        {
            SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition = new Vector3(1, 0.458f, SubCarsRen[j].transform.localPosition.z);
        }
        TempSpeeds1 = new float[8];
        List<float> genSpeeds = new List<float>();
        for (int i = 0; i < 8; i++)
        {
            float gen = Rnd.Range(0.12f, 0.22f);
            while (ValueTooClose(genSpeeds, gen, .008f))
                gen = Rnd.Range(0.12f, 0.22f);
            genSpeeds.Add(gen);
        }
        genSpeeds.Sort();
        int bracket1;
        int bracket2;
        int bracket3;
        int bracket4;
        int bracket5;
        int bracket6;
        int loser1;
        int loser2;
        int loser3;
        int loser4;
        int loser5;
        int loser6;
        int loseBracket1;
        int loseBracket2;
        int loseLoser1;
        int loseLoser2;
        if (GetCarsAhead(submitOrder[0]).Contains(submitOrder[1]))
        {
            bracket1 = 1;
            loser1 = 0;
        }
        else
        {
            bracket1 = 0;
            loser1 = 1;
        }
        if (GetCarsAhead(submitOrder[2]).Contains(submitOrder[3]))
        {
            bracket2 = 3;
            loser2 = 2;
        }
        else
        {
            bracket2 = 2;
            loser2 = 3;
        }
        if (GetCarsAhead(submitOrder[4]).Contains(submitOrder[5]))
        {
            bracket3 = 5;
            loser3 = 4;
        }
        else
        {
            bracket3 = 4;
            loser3 = 5;
        }
        if (GetCarsAhead(submitOrder[6]).Contains(submitOrder[7]))
        {
            bracket4 = 7;
            loser4 = 6;
        }
        else
        {
            bracket4 = 6;
            loser4 = 7;
        }
        if (GetCarsAhead(submitOrder[bracket1]).Contains(submitOrder[bracket2]))
        {
            bracket5 = bracket2;
            loser5 = bracket1;
        }
        else
        {
            bracket5 = bracket1;
            loser5 = bracket2;
        }
        if (GetCarsAhead(submitOrder[bracket3]).Contains(submitOrder[bracket4]))
        {
            bracket6 = bracket4;
            loser6 = bracket3;
        }
        else
        {
            bracket6 = bracket3;
            loser6 = bracket4;
        }
        if (GetCarsAhead(submitOrder[bracket5]).Contains(submitOrder[bracket6]))
        {
            TempSpeeds1[bracket6] = genSpeeds[7];
            TempSpeeds1[bracket5] = genSpeeds[6];
        }
        else
        {
            TempSpeeds1[bracket5] = genSpeeds[7];
            TempSpeeds1[bracket6] = genSpeeds[6];
        }
        if (GetCarsAhead(submitOrder[loser5]).Contains(submitOrder[loser6]))
        {
            TempSpeeds1[loser6] = genSpeeds[5];
            TempSpeeds1[loser5] = genSpeeds[4];
        }
        else
        {
            TempSpeeds1[loser5] = genSpeeds[5];
            TempSpeeds1[loser6] = genSpeeds[4];
        }
        if (GetCarsAhead(submitOrder[loser1]).Contains(submitOrder[loser2]))
        {
            loseBracket1 = loser2;
            loseLoser1 = loser1;
        }
        else
        {
            loseBracket1 = loser1;
            loseLoser1 = loser2;
        }
        if (GetCarsAhead(submitOrder[loser3]).Contains(submitOrder[loser4]))
        {
            loseBracket2 = loser4;
            loseLoser2 = loser3;
        }
        else
        {
            loseBracket2 = loser3;
            loseLoser2 = loser4;
        }
        if (GetCarsAhead(submitOrder[loseLoser1]).Contains(submitOrder[loseLoser2]))
        {
            TempSpeeds1[loseLoser2] = genSpeeds[3];
            TempSpeeds1[loseLoser1] = genSpeeds[2];
        }
        else
        {
            TempSpeeds1[loseLoser1] = genSpeeds[3];
            TempSpeeds1[loseLoser2] = genSpeeds[2];
        }
        if (GetCarsAhead(submitOrder[loseBracket1]).Contains(submitOrder[loseBracket2]))
        {
            TempSpeeds1[loseBracket2] = genSpeeds[1];
            TempSpeeds1[loseBracket1] = genSpeeds[0];
        }
        else
        {
            TempSpeeds1[loseBracket1] = genSpeeds[1];
            TempSpeeds1[loseBracket2] = genSpeeds[0];
        }
        for (int i = 0; i < 30; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
            }
            yield return new WaitForSeconds(.03f);
        }
        yield return new WaitForSeconds(.2f);
        SubRoadsRen.sprite = AOneByOnePixelBlackSquare;
        GoalLine.gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);
        if (correct)
        {
            SolveText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.15f);
            adjustableAudio.clip = adjustableClips[3];
            adjustableAudio.Play();
            if (adjustableAudio.mute)
            {
                StopCoroutine(MuteCo);
                muteText.text = "";
            }
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            Debug.LogFormat("[The Midnight Motorist #{0}] Module solved", moduleId);
        }
        else
        {
            StrikeText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.15f);
            GetComponent<KMBombModule>().HandleStrike();
            adjustableAudio.clip = adjustableClips[4];
            adjustableAudio.Play();
            yield return new WaitForSeconds(5.85f);
            GenerateSubmission(true);
            StrikeText.gameObject.SetActive(false);
            SubRoadsRen.sprite = SubRoadsSpr[0];
            RoadDelay = .1f;
            RoadIndex = 0;
            GoalLine.transform.localPosition = new Vector3(-1.25f, 0.458f, -0.252f);
            GoalLine.gameObject.SetActive(true);
            SubCarsRen[0].transform.localPosition = new Vector3(0.768f, 0.458f, -0.8399222f);
            SubCarsRen[1].transform.localPosition = new Vector3(0.768f, 0.458f, -0.679518f);
            SubCarsRen[2].transform.localPosition = new Vector3(0.768f, 0.458f, -0.5161136f);
            SubCarsRen[3].transform.localPosition = new Vector3(0.768f, 0.458f, -0.3561094f);
            SubCarsRen[4].transform.localPosition = new Vector3(0.768f, 0.458f, -0.1950051f);
            SubCarsRen[5].transform.localPosition = new Vector3(0.768f, 0.458f, -0.02900078f);
            SubCarsRen[6].transform.localPosition = new Vector3(0.768f, 0.458f, 0.1310035f);
            SubCarsRen[7].transform.localPosition = new Vector3(0.768f, 0.458f, 0.2910077f);
            Selector.SetActive(true);
        }
        animatingRace = false;
    }

    IEnumerator MoveLeftStick()
    {
        while (true)
        {
            //Debug.Log(LeftStickGO.transform.localEulerAngles.x);
            if (MoveLeftUp)
            {
                if (LeftStickGO.transform.localEulerAngles.x < 30f || LeftStickGO.transform.localEulerAngles.x > 329f)
                {
                    MoveLeftRegister = false;
                    LeftStickGO.transform.Rotate(new Vector3(3f, 0, 0));
                }
                else if (!MoveLeftRegister)
                {
                    MoveLeftRegister = true;
                    TickGoBrrrrr = StartCoroutine(MoveSelectionTick("up"));
                }
                if (LeftStickGO.transform.localEulerAngles.x >= 30f && LeftStickGO.transform.localEulerAngles.x < 300f)
                {
                    MaxLeftJoystickDistance = true;
                }
                else
                {
                    MaxLeftJoystickDistance = false;
                    if (TickGoBrrrrr != null)
                    {
                        StopCoroutine(TickGoBrrrrr);
                    }
                }
            }
            else if (MoveLeftDown)
            {
                if (LeftStickGO.transform.localEulerAngles.x > 330 || LeftStickGO.transform.localEulerAngles.x < 31)
                {
                    MoveLeftRegister = false;
                    LeftStickGO.transform.Rotate(new Vector3(-3f, 0, 0));
                }
                else if (!MoveLeftRegister)
                {
                    MoveLeftRegister = true;
                    TickGoBrrrrr = StartCoroutine(MoveSelectionTick("down"));
                }
                if (LeftStickGO.transform.localEulerAngles.x <= 330 && LeftStickGO.transform.localEulerAngles.x > 40f)
                {
                    MaxLeftJoystickDistance = true;
                }
                else
                {
                    MaxLeftJoystickDistance = false;
                    if (TickGoBrrrrr != null)
                    {
                        StopCoroutine(TickGoBrrrrr);
                    }
                }
            }
            else if (!MoveLeftDown && !MoveLeftUp)
            {
                MoveLeftRegister = false;
                if (LeftStickGO.transform.localEulerAngles.x > 0 && LeftStickGO.transform.localEulerAngles.x < 36)
                {
                    LeftStickGO.transform.localEulerAngles += new Vector3(-3f, 0, 0);
                    //LeftStickGO.transform.Rotate(new Vector3(-3f, 0, 0));
                }
                else if (LeftStickGO.transform.localEulerAngles.x > 300)
                {
                    //LeftStickGO.transform.Rotate(new Vector3(3f, 0, 0));
                    LeftStickGO.transform.localEulerAngles += new Vector3(3f, 0, 0);
                }
                if (Math.Abs(1 - LeftStickGO.transform.localEulerAngles.x) > 0 && LeftStickGO.transform.localEulerAngles.x < 1)
                {
                    LeftStickGO.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
            }
            yield return new WaitForSeconds(.01f);
        }
    }

    IEnumerator MoveRightStick()
    {
        while (true)
        {
            //Debug.Log(RightStickGO.transform.localEulerAngles.x);
            if (MoveRightUp)
            {
                if (RightStickGO.transform.localEulerAngles.x < 30f || RightStickGO.transform.localEulerAngles.x > 329f)
                {
                    MoveRightRegister = false;
                    RightStickGO.transform.Rotate(new Vector3(3f, 0, 0));
                }
                else if (!MoveRightRegister)
                {
                    MoveRightRegister = true;
                    TickGoBrrrrr2 = StartCoroutine(MoveAudioTick("up"));
                }
                if (RightStickGO.transform.localEulerAngles.x >= 30f && RightStickGO.transform.localEulerAngles.x < 300f)
                {
                    MaxRightJoystickDistance = true;
                }
                else
                {
                    MaxRightJoystickDistance = false;
                    if (TickGoBrrrrr2 != null)
                    {
                        StopCoroutine(TickGoBrrrrr2);
                    }
                }
            }
            else if (MoveRightDown)
            {
                if (RightStickGO.transform.localEulerAngles.x > 330 || RightStickGO.transform.localEulerAngles.x < 31)
                {
                    MoveRightRegister = false;
                    RightStickGO.transform.Rotate(new Vector3(-3f, 0, 0));
                }
                else if (!MoveRightRegister)
                {
                    MoveRightRegister = true;
                    TickGoBrrrrr2 = StartCoroutine(MoveAudioTick("down"));
                }
                if (RightStickGO.transform.localEulerAngles.x <= 330 && RightStickGO.transform.localEulerAngles.x > 40f)
                {
                    MaxRightJoystickDistance = true;
                }
                else
                {
                    MaxRightJoystickDistance = false;
                    if (TickGoBrrrrr2 != null)
                    {
                        StopCoroutine(TickGoBrrrrr2);
                    }
                }
            }
            else if (!MoveRightDown && !MoveRightUp)
            {
                MoveRightRegister = false;
                if (RightStickGO.transform.localEulerAngles.x > 0 && RightStickGO.transform.localEulerAngles.x < 36)
                {
                    RightStickGO.transform.localEulerAngles += new Vector3(-3f, 0, 0);
                    //RightStickGO.transform.Rotate(new Vector3(-3f, 0, 0));
                }
                else if (RightStickGO.transform.localEulerAngles.x > 300)
                {
                    //RightStickGO.transform.Rotate(new Vector3(3f, 0, 0));
                    RightStickGO.transform.localEulerAngles += new Vector3(3f, 0, 0);
                }
                if (Math.Abs(1 - RightStickGO.transform.localEulerAngles.x) > 0 && RightStickGO.transform.localEulerAngles.x < 1)
                {
                    RightStickGO.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
            }
            yield return new WaitForSeconds(.01f);
        }
    }

    IEnumerator MoveSelectionTick(string dir)
    {
        while (MaxLeftJoystickDistance && !animatingRace && submissionMode)
        {
            if (dir == "up")
            {
                currentSelection -= 1;
                if (currentSelection == -1)
                    currentSelection = raceOrder.Length - 1;
                Selector.transform.localPosition = new Vector3(0.618f, SubCarsRen[currentSelection].transform.localPosition.y, SubCarsRen[currentSelection].transform.localPosition.z);
            }
            else
            {
                currentSelection += 1;
                if (currentSelection == raceOrder.Length)
                    currentSelection = 0;
                Selector.transform.localPosition = new Vector3(0.618f, SubCarsRen[currentSelection].transform.localPosition.y, SubCarsRen[currentSelection].transform.localPosition.z);
            }
            yield return new WaitForSeconds(.25f);
        }
    }

    IEnumerator MoveAudioTick(string dir)
    {
        while (MaxRightJoystickDistance && !moduleSolved)
        {
            if (dir == "up")
            {
                if (adjustableAudio.volume < 1)
                    adjustableAudio.volume += .1f;
                volumeText.text = "VOL: " + Math.Round(adjustableAudio.volume * 100, 1) + "%";
            }
            else
            {
                if (adjustableAudio.volume > 0)
                    adjustableAudio.volume -= .1f;
                volumeText.text = "VOL: " + Math.Round(adjustableAudio.volume * 100, 1) + "%";
            }
            yield return new WaitForSeconds(.25f);
        }
    }

    IEnumerator MuteTick()
    {
        while (true)
        {
            muteText.text = "MUTE";
            yield return new WaitForSeconds(.5f);
            muteText.text = "";
            yield return new WaitForSeconds(.5f);
        }
    }

    #endregion

    private void Update()
    {
        if (CanMoveLeftStick && !TwitchPlaysActive)
        {
            if (MouseLeftPos == new Vector3(-1000, -1000, -1000))
            {
                MouseLeftPos = Input.mousePosition;
            }
            if (Input.mousePosition.y > MouseLeftPos.y)
            {
                MoveLeftUp = true;
                MoveLeftDown = false;
            }
            else if (Input.mousePosition.y < MouseLeftPos.y)
            { //elif necessary so that if player does not move cursor, stick does not go up
                MoveLeftDown = true;
                MoveLeftUp = false;
            }
        }
        if (!CanMoveLeftStick && !TwitchPlaysActive)
        {
            MouseLeftPos = new Vector3(-1000, -1000, -1000);
            MoveLeftUp = false;
            MoveLeftDown = false;
        }
        if (CanMoveRightStick && !TwitchPlaysActive)
        {
            if (MouseRightPos == new Vector3(-1000, -1000, -1000))
            {
                MouseRightPos = Input.mousePosition;
            }
            if (Input.mousePosition.y > MouseRightPos.y)
            {
                MoveRightUp = true;
                MoveRightDown = false;
            }
            else if (Input.mousePosition.y < MouseRightPos.y)
            { //elif necessary so that if player does not move cursor, stick does not go up
                MoveRightDown = true;
                MoveRightUp = false;
            }
        }
        if (!CanMoveRightStick && !TwitchPlaysActive)
        {
            MouseRightPos = new Vector3(-1000, -1000, -1000);
            MoveRightUp = false;
            MoveRightDown = false;
        }
    }

    //twitch plays
    bool TwitchPlaysActive;
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <yellow/y/blue/b> [Presses the yellow or blue button on the left] | !{0} select <1-8> [Selects the specified car from top to bottom if in the submit phase]";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify which button to press!";
            else
            {
                if (parameters[1].ToLowerInvariant().EqualsAny("yellow", "y"))
                {
                    yield return null;
                    if (submissionMode && currentSelection == correctCar && !animatingRace)
                        yield return "solve";
                    else if (submissionMode && currentSelection != correctCar && !animatingRace)
                        yield return "strike";
                    buttons[0].OnInteract();
                }
                else if (parameters[1].ToLowerInvariant().EqualsAny("blue", "b"))
                {
                    yield return null;
                    buttons[1].OnInteract();
                }
                else
                    yield return "sendtochaterror The specified button '" + parameters[1] + "' is invalid!";
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*select\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify which car to select!";
            else
            {
                if (parameters[1].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8"))
                {
                    if (!submissionMode)
                    {
                        yield return "sendtochaterror You must be in the submit phase to do this!";
                        yield break;
                    }
                    yield return null;
                    int index1 = currentSelection;
                    int index2 = currentSelection;
                    int ct1 = 0;
                    int ct2 = 0;
                    while (index1 != int.Parse(parameters[1]) - 1)
                    {
                        index1++;
                        if (index1 == raceOrder.Length)
                            index1 = 0;
                        ct1++;
                    }
                    while (index2 != int.Parse(parameters[1]) - 1)
                    {
                        index2--;
                        if (index2 == -1)
                            index2 = raceOrder.Length - 1;
                        ct2++;
                    }
                    if (ct1 < ct2)
                    {
                        LeftStick.OnInteract();
                        MoveLeftDown = true;
                        while (currentSelection != int.Parse(parameters[1]) - 1) yield return null;
                        MoveLeftDown = false;
                        LeftStick.OnInteractEnded();
                    }
                    else if (ct1 > ct2)
                    {
                        LeftStick.OnInteract();
                        MoveLeftUp = true;
                        while (currentSelection != int.Parse(parameters[1]) - 1) yield return null;
                        MoveLeftUp = false;
                        LeftStick.OnInteractEnded();
                    }
                    else
                    {
                        int choice = Rnd.Range(0, 2);
                        LeftStick.OnInteract();
                        if (choice == 0)
                            MoveLeftDown = true;
                        else
                            MoveLeftUp = true;
                        while (currentSelection != int.Parse(parameters[1]) - 1) yield return null;
                        if (choice == 0)
                            MoveLeftDown = false;
                        else
                            MoveLeftUp = false;
                        LeftStick.OnInteractEnded();
                    }
                }
                else
                    yield return "sendtochaterror The specified car '" + parameters[1] + "' is invalid!";
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (animatingRace && submissionMode && currentSelection != correctCar)
        {
            StopAllCoroutines();
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
        }
        if (animatingRace && !submissionMode)
        {
            buttons[0].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        if (!animatingRace)
        {
            if (!submissionMode)
            {
                buttons[1].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            int index1 = currentSelection;
            int index2 = currentSelection;
            int ct1 = 0;
            int ct2 = 0;
            while (index1 != correctCar)
            {
                index1++;
                if (index1 == raceOrder.Length)
                    index1 = 0;
                ct1++;
            }
            while (index2 != correctCar)
            {
                index2--;
                if (index2 == -1)
                    index2 = raceOrder.Length - 1;
                ct2++;
            }
            if (ct1 < ct2)
            {
                LeftStick.OnInteract();
                MoveLeftDown = true;
                while (currentSelection != correctCar) yield return null;
                MoveLeftDown = false;
                LeftStick.OnInteractEnded();
            }
            else if (ct1 > ct2)
            {
                LeftStick.OnInteract();
                MoveLeftUp = true;
                while (currentSelection != correctCar) yield return null;
                MoveLeftUp = false;
                LeftStick.OnInteractEnded();
            }
            else
            {
                int choice = Rnd.Range(0, 2);
                LeftStick.OnInteract();
                if (choice == 0)
                    MoveLeftDown = true;
                else
                    MoveLeftUp = true;
                while (currentSelection != correctCar) yield return null;
                if (choice == 0)
                    MoveLeftDown = false;
                else
                    MoveLeftUp = false;
                LeftStick.OnInteractEnded();
            }
            buttons[0].OnInteract();
        }
        while (!SolveText.gameObject.activeSelf) yield return true;
    }
}