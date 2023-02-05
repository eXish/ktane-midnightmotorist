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
   public KMSelectable LBlueButton;
   public KMSelectable LYellowButton;

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

   private char[] raceOrder = new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'P', 'W' };
   private char[] currentRace = new char[4];
   private int correctCar;
   private bool playingRace;

   private float RoadDelay = .1f; //This is the delay that is in between each change of road
   private int RoadIndex = 0; //Index of sprite

   static int moduleIdCounter = 1;
   int moduleId;
   private bool moduleSolved;

   Coroutine RoadGoBrrrrr;

   void Awake () {
      moduleId = moduleIdCounter++;
      foreach (KMSelectable obj in buttons) {
         KMSelectable pressed = obj;
         pressed.OnInteract += delegate () { PressButton(pressed); return false; };
      }

      LeftStick.OnInteract += delegate () { StickPress(); return false; };
      LeftStick.OnInteractEnded += delegate () { StickRelease(); };
   }

   void Start () {
      raceOrder = raceOrder.Shuffle();
      Debug.LogFormat("[The Midnight Motorist #{0}] Car color order: {1}", moduleId, raceOrder.Join(""));
      GenerateRace();
      RoadGoBrrrrr = StartCoroutine(ChangeRoad());
      StartCoroutine(MoveStick(LeftStickGO));
      StartCoroutine(ShowTestRace());
   }

   void GenerateRace () {
      List<char> usedCars = new List<char>();
      while (usedCars.Count != 4) {
         int choice = UnityEngine.Random.Range(0, raceOrder.Length);
         while (usedCars.Contains(raceOrder[choice])) {
            choice = UnityEngine.Random.Range(0, raceOrder.Length);
         }
         usedCars.Add(raceOrder[choice]);
      }
      for (int i = 0; i < 4; i++) {
         currentRace[i] = usedCars[i];
      }
      for (int i = 0; i < 4; i++) {
         TestCarsRen[i].sprite = CarsSpr[Array.IndexOf(raceOrder, currentRace[i])];
      }
      for (int i = raceOrder.Length - 1; i >= 0; i--) {
         for (int j = 0; j < 4; j++) {
            if (currentRace[j] == raceOrder[i]) {
               correctCar = j;
               return;
            }
         }
      }
   }

   void PressButton (KMSelectable pressed) {
      if (moduleSolved != true) {
         pressed.AddInteractionPunch();
         audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
         int index = Array.IndexOf(buttons, pressed);
         if (index == 0) {
            if (!playingRace) {
               playingRace = true;
               //Start animation business
            }
            else {
               playingRace = false;
               //Stop animation business
               GenerateRace();
            }
         }
         else if (index - 1 == correctCar) {
            Debug.LogFormat("[The Midnight Motorist #{0}] Car colors from top to bottom: {1}", moduleId, currentRace.Join(""));
            Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is correct", moduleId, currentRace[index - 1]);
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            Debug.LogFormat("[The Midnight Motorist #{0}] Module solved", moduleId);
         }
         else {
            Debug.LogFormat("[The Midnight Motorist #{0}] Car colors from top to bottom: {1}", moduleId, currentRace.Join(""));
            Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is incorrect", moduleId, currentRace[index - 1]);
            GetComponent<KMBombModule>().HandleStrike();
            GenerateRace();
         }
      }
   }

   void StickPress () {
      CanMoveStick = true;
   }

   void StickRelease () {
      CanMoveStick = false;
   }

   #region Animation

   IEnumerator ChangeRoad () {
      while (true) {
         if (TestRoadsRen[0].gameObject.activeSelf) {
            TestRoadsRen[0].sprite = TestRoadsSpr[RoadIndex];
            TestRoadsRen[1].sprite = TestRoadsSpr[RoadIndex];
         }
         else {
            SubRoadsRen.sprite = SubRoadsSpr[RoadIndex];
         }
         
         yield return new WaitForSecondsRealtime(RoadDelay);
         RoadIndex = (RoadIndex + 1) % 3;
      }
   }

   void RaceResult (SpriteRenderer First, SpriteRenderer Second, SpriteRenderer Third, SpriteRenderer Last) {
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
   }

   IEnumerator ShowTestRace () {
      for (int i = 0; i < 20; i++) {
         for (int j = 0; j < 4; j++) {
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(.056f, 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      float[] TempSpeeds1 = { Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f) };
      for (int i = 0; i < 20; i++) {
         for (int j = 0; j < 4; j++) {
            
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      RoadDelay /= 5;
      TempSpeeds1 = new float[] { Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f) };
      for (int i = 0; i < 30; i++) {
         
         for (int j = 0; j < 4; j++) {
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      yield return new WaitForSeconds(.9f);
      for (int i = 0; i < 25; i++) {
         GoalLine.transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(.02f, 0, 0);
         yield return new WaitForSeconds(.01f);
      }
      StopCoroutine(RoadGoBrrrrr);
      for (int j = 0; j < 4; j++) {
         TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition = new Vector3(1, 0.458f, TestCarsRen[j].transform.localPosition.z);
      }
      TempSpeeds1 = new float[] { Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f) };
      for (int i = 0; i < 30; i++) {

         for (int j = 0; j < 4; j++) {
            
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds(.03f);
      }
   }

   IEnumerator MoveStick (GameObject Stick) {
      while (true) {
         //Debug.Log(Stick.transform.localEulerAngles.x);
         if (MoveUp && (Stick.transform.localEulerAngles.x < 30f || Stick.transform.localEulerAngles.x > 329f)) { 
            Stick.transform.Rotate(new Vector3(3f, 0, 0));
         }
         else if (MoveDown && (Stick.transform.localEulerAngles.x > 330 || Stick.transform.localEulerAngles.x < 31)) {
            Stick.transform.Rotate(new Vector3(-3f, 0, 0));
         }
         else if (!MoveDown && !MoveUp) {
            if (Stick.transform.localEulerAngles.x > 0 && Stick.transform.localEulerAngles.x < 31) {
               Stick.transform.localEulerAngles += new Vector3(-3f, 0, 0);
               //LeftStickGO.transform.Rotate(new Vector3(-3f, 0, 0));
            }
            else if (Stick.transform.localEulerAngles.x > 329) {
               //LeftStickGO.transform.Rotate(new Vector3(3f, 0, 0));
               Stick.transform.localEulerAngles += new Vector3(3f, 0, 0);
            }
            if (Math.Abs(1 - Stick.transform.localEulerAngles.x) > 0 && Stick.transform.localEulerAngles.x < 1) {
               Stick.transform.localEulerAngles = new Vector3(0, 0, 0);
            }
         }
         yield return new WaitForSeconds(.01f);
      }
   }

   #endregion

   private void Update () {
      if (CanMoveStick) {
         if (MousePos == new Vector3(-1000, -1000, -1000)) {
            MousePos = Input.mousePosition;
         }
         if (Input.mousePosition.y > MousePos.y) {
            MoveUp = true;
            MoveDown = false;
         }
         else if (Input.mousePosition.y < MousePos.y) { //elif necessary so that if player does not move cursor, stick does not go up
            MoveDown = true;
            MoveUp = false;
         }
      }
      if (!CanMoveStick) {
         MousePos = new Vector3(-1000, -1000, -1000);
         MoveUp = false;
         MoveDown = false;
      }
   }

   //twitch plays
#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"!{0} something [Does something]";
#pragma warning restore 414
   IEnumerator ProcessTwitchCommand (string command) {
      if (Regex.IsMatch(command, @"^\s*something\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) {
         yield return null;
         Debug.Log("Did something");
         yield break;
      }
   }
}