using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;
//using Bingus;

public class TheMidnightMotoristScript : MonoBehaviour {

   public KMAudio audio;
   public KMBombInfo bomb;
   public KMSelectable[] buttons;

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

   void Awake () {
      moduleId = moduleIdCounter++;
      foreach (KMSelectable obj in buttons) {
         KMSelectable pressed = obj;
         pressed.OnInteract += delegate () { PressButton(pressed); return false; };
      }
   }

   void Start () {
      raceOrder = raceOrder.Shuffle();
      Debug.LogFormat("[The Midnight Motorist #{0}] Car color order: {1}", moduleId, raceOrder.Join(""));
      GenerateRace();
      StartCoroutine(ChangeRoad());
      RaceResult(TestCarsRen[0], TestCarsRen[1], TestCarsRen[2], TestCarsRen[3]);
   }

   void GenerateRace () {
      List<char> usedCars = new List<char>();
      while (usedCars.Count != 4) {
         int choice = UnityEngine.Random.Range(0, raceOrder.Length);
         while (usedCars.Contains(raceOrder[choice]))
            choice = UnityEngine.Random.Range(0, raceOrder.Length);
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

   #region

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

   #endregion

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