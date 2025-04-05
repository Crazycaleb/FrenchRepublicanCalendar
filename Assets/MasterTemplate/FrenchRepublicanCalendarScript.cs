using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class FrenchRepublicanCalendarScript : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   public TextMesh Month;

   public Material[] LedColor;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;
   int[,] calendar =
   {{1,2,3,4,5,6,7,8,9,10},
   {11,12,13,14,15,16,17,18,19,20},
   {21,22,23,24,25,26,27,28,29,30}}; //Grid for row and column matching.

   void Awake () {
      ModuleId = ModuleIdCounter++;
      int markedMonth = Rnd.Range(1, 13); //Generate a month and day for the marked spot on the calender.
      int markedDay = Rnd.Range(1, 31);
      /*
      foreach (KMSelectable object in keypad) {
          object.OnInteract += delegate () { keypadPress(object); return false; };
      }
      */

      //button.OnInteract += delegate () { buttonPress(); return false; };
      Start();

   }

   void Start () {
      DateConverter();
      DaySolution();
      MonthSolution();
   }

   void DateConverter(){
      DateTime currentDate = DateTime.Now;
      DateTime republicanYearStart = GetRepublicanYearStart(currentDate.Year);

      if (currentDate < republicanYearStart)
      {
         republicanYearStart = GetRepublicanYearStart(currentDate.Year - 1);
      }

      int republicanYear = republicanYearStart.Year - 1792 + 1;
      int dayOfYear = (currentDate - republicanYearStart).Days + 1;


      // Adjust for leap years in the Gregorian calendar
      if (DateTime.IsLeapYear(currentDate.Year) && currentDate > new DateTime(currentDate.Year, 2, 28))
      {
         dayOfYear += 1;
      }

      string[] republicanMonths = {
            "Vendémiaire", "Brumaire", "Frimaire", "Nivôse", "Pluviôse", "Ventôse",
            "Germinal", "Floréal", "Prairial", "Messidor", "Thermidor", "Fructidor",
            "Sans-culottides"
        };

      string[] complementaryDays = {
            "La Fête de la Vertu", "La Fête du Génie", "La Fête du Travail",
            "La Fête de l'Opinion", "La Fête des Récompenses", "La Fête de la Révolution"
        };

      int monthIndex;
      int dayInMonth;
      string republicanMonth;

      if (dayOfYear > 360)
      {
         // Complementary days
         monthIndex = 12; // Sans-culottides
         dayInMonth = dayOfYear - 360;
         if (dayInMonth <= complementaryDays.Length)
         {
            republicanMonth = complementaryDays[dayInMonth - 1];
            Debug.Log("Current Date: " + currentDate.ToString("yyyy-MM-dd"));
            Debug.Log("French Republican Calendar Date: " + republicanYear + ", " + republicanMonth);
            return;
         }
         else
         {
            Debug.Log("Error: Invalid day in Sans-culottides");
            return;
         }
      }
      else
      {
         monthIndex = (dayOfYear - 1) / 30;
         dayInMonth = (dayOfYear - 1) % 30 + 1;
         republicanMonth = republicanMonths[monthIndex];
      }

      Debug.Log("Current Date: " + currentDate.ToString("yyyy-MM-dd"));
      Debug.Log("French Republican Calendar Date: Year " + republicanMonth + " " + dayInMonth);
   }

   static DateTime GetRepublicanYearStart(int year)
   {
      DateTime september22 = new DateTime(year, 9, 22);
      return september22;
   }


   void DaySolution(){

   }

   void MonthSolution(){

   }


#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
   }

   IEnumerator TwitchHandleForcedSolve () {
      yield return null;
   }
}
