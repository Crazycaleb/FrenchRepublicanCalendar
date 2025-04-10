using Assets;
using KModkit;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class FrenchRepublicanCalendarScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;

    public TextMesh Month;

    public KMSelectable[] Arrows;

    public GameObject[] DayObjects;
    private DayStruct[] dayStructs;

    public Material[] LedColors;
    public MeshRenderer led;
    public int ledIndex;

    public Sprite[] TableLayouts;
    public SpriteRenderer Table;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;
    int[,] calendar =
    {{1,2,3,4,5,6,7,8,9,10},
   {11,12,13,14,15,16,17,18,19,20},
   {21,22,23,24,25,26,27,28,29,30}}; //Grid for row and column matching.

    private RepublicanDate actualDate;

    /// <summary>
    /// 0-indexed
    /// </summary>
    private int circledDay;
    /// <summary>
    /// 0-indexed
    /// </summary>
    private int circledMonth;

    private int[] targetDays;
    private RepublicanMonth targetMonth;

    int displayedMonth = 0;
    static string[] republicanMonths = {
        "Vendémiaire", "Brumaire", "Frimaire", "Nivôse", "Pluviôse", "Ventôse",
        "Germinal", "Floréal", "Prairial", "Messidor", "Thermidor", "Fructidor",
        "Sans-culottides"
    };

    static string[] complementaryDays = {
        "La Fête de la Vertu", "La Fête du Génie", "La Fête du Travail",
        "La Fête de l'Opinion", "La Fête des Récompenses", "La Fête de la Révolution"
    };

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        int markedMonth = Rnd.Range(1, 13); //Generate a month and day for the marked spot on the calender.
        int markedDay = Rnd.Range(1, 31);
        Arrows[0].OnInteract += delegate () { MoveArrow(false); return false; };
        Arrows[1].OnInteract += delegate () { MoveArrow(true); return false; };

        dayStructs = DayObjects.Select(obj => new DayStruct()
        {
            selectable = obj.GetComponentInChildren<KMSelectable>(),
            circle = obj.GetComponentInChildren<SpriteRenderer>()
        }).ToArray();
        int i = 1;
        foreach (DayStruct obj in dayStructs)
        {
            obj.circle.enabled = false;
            obj.index = i;
            obj.selectable.OnInteract += delegate ()
            {
                PressDay(obj.index);
                return false;
            };
            i++;
        }
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */

        //button.OnInteract += delegate () { buttonPress(); return false; };
    }

    private void PressDay(int i)
    {
        dayStructs[i - 1].selectable.AddInteractionPunch(.2f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if(targetDays.Contains(i) && (int)targetMonth == displayedMonth)
        {
            Module.HandlePass();
        }
        else
        {
            Module.HandleStrike();
        }
    }

    void Start()
    {
        GenerateCircledDay();

        DaySolution();
        MonthSolution();

        displayedMonth = Rnd.Range(0, republicanMonths.Length);
        UpdateDisplay();
    }

    private void GenerateCircledDay()
    {
        circledMonth = Rnd.Range(0, republicanMonths.Length);
        circledDay = Rnd.Range(0, circledMonth == 12 ? 6 : 31);
        Log(string.Format("Circled Date is {0} {1}", republicanMonths[circledMonth], circledDay + 1));
    }

    private void UpdateDisplay()
    {
        Month.text = republicanMonths[displayedMonth].ToUpper();
        dayStructs[circledDay].circle.enabled = displayedMonth == circledMonth;
        Table.sprite = TableLayouts[displayedMonth == republicanMonths.Length - 1 ? 1 : 0];
        foreach (var dayObject in DayObjects.Skip(6))
        {
            dayObject.SetActive(displayedMonth != republicanMonths.Length - 1);
        }
    }

    void MoveArrow(bool isForward)
    {
        displayedMonth += isForward ? 1 : -1;
        if (displayedMonth == -1)
            displayedMonth = republicanMonths.Length - 1;
        else
            displayedMonth %= republicanMonths.Length;
        UpdateDisplay();
    }

    void GetCurrentRepublicanDay()
    {
        DateTime currentDate = DateTime.Now;
        DateTime republicanYearStart = GetRepublicanYearStart(currentDate.Year);

        if (currentDate < republicanYearStart)
        {
            republicanYearStart = GetRepublicanYearStart(currentDate.Year - 1);
        }

        int republicanYear = republicanYearStart.Year - 1791; //1792 is year 1
        int dayOfYear = (currentDate - republicanYearStart).Days + 1;


        // Adjust for leap years in the Gregorian calendar
        if (DateTime.IsLeapYear(currentDate.Year) && currentDate > new DateTime(currentDate.Year, 2, 28))
        {
            dayOfYear += 1;
        }

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
                /*Debug.Log("Current Date: " + currentDate.ToString("yyyy-MM-dd"));
                Debug.Log("French Republican Calendar Date: " + republicanYear + ", " + republicanMonth);
                return new RepublicanDay { Month = dayInMonth, Day = dayInMonth };*/
            }
            else
            {
                Debug.Log("Error: Invalid day in Sans-culottides");
                throw new ArgumentException();
            }
        }
        else
        {
            monthIndex = (dayOfYear - 1) / 30;
            dayInMonth = (dayOfYear - 1) % 30 + 1;
            republicanMonth = republicanMonths[monthIndex];
        }

        Log("Current Date: " + currentDate.ToString("yyyy-MM-dd"));
        Log($"French Republican Calendar Date: {republicanYear} {republicanMonth} {dayInMonth}");
        actualDate = new RepublicanDate { Month = monthIndex, Day = dayInMonth };
    }

    static DateTime GetRepublicanYearStart(int year)
    {
        DateTime september22 = new DateTime(year, 9, 22);
        return september22;
    }


    void DaySolution()
    {
        ledIndex = Rnd.Range(0, LedColors.Length);
        led.material = LedColors[ledIndex];

        int circledDayHumanIndexed = circledDay + 1;

        GetCurrentRepublicanDay();
        //Spe Sans-Cullotides
        if (circledMonth == 12)
        {
            switch (circledDay)
            {
                case 0:
                case 3:
                    circledDayHumanIndexed = Bomb.GetModuleIDs().Count();
                    break;
                case 1:
                case 4:
                    circledDayHumanIndexed = Bomb.GetBatteryCount() + Bomb.GetIndicators().SelectMany(i => i.ToCharArray()).Where(c => "AEIOU".Contains(c)).Count() + Bomb.GetPortCount();
                    break;
                case 2:
                case 5:
                    circledDayHumanIndexed = Bomb.GetSerialNumberNumbers().Sum();
                    break;
            }
            circledDayHumanIndexed = (circledDayHumanIndexed % 30) + 1;
        }

        Log($"Led color is {new string[] { "red", "yellow", "green", "blue" }[ledIndex]}");

        switch (ledIndex)
        {
            case 0: //red
                targetDays = new int[] { 10 * ((actualDate.Day - 1) / 10) + (circledDay % 10) + 1 };
                break;
            case 1: //yellow
                targetDays = new int[] { 10 * (circledDay / 10) + ((actualDate.Day - 1) % 10) + 1 };
                break;
            case 2: //green
                decimal avg = (circledDayHumanIndexed + actualDate.Day) / 2m;
                if (avg == Math.Round(avg))
                    targetDays = new int[] { (int)avg };
                else
                    targetDays = new int[] { (int)Math.Floor(avg), (int)Math.Ceiling(avg) };
                break;
            case 3: //blue
                int total = circledDayHumanIndexed + actualDate.Day;
                if (total > 30) total -= 30;
                targetDays = new int[] { total };
                break;
        }

        Log($"Valid days are {string.Join(",", targetDays.Select(n => n.ToString()).ToArray())}");
    }

    void MonthSolution()
    {
        RepublicanDayName circledDayName = Data.Get(circledDay + 1, (RepublicanMonth)circledMonth);
        RepublicanDayName actualDayName = Data.Get(actualDate.Day, (RepublicanMonth)(actualDate.Month - 1));

        if (actualDate.Day % 2 == 1) //If the day of the week for the current date is odd
        {
            Season[] targetSeasons = new Season[] { Season.Autumn, Season.Winter };
            bool hasTargetSeason = targetSeasons.Contains(((RepublicanMonth)circledMonth).GetSeason());

            bool serialNumberHasActualDay = Bomb.GetSerialNumberLetters().Any(sn => actualDayName.Name.Contains(sn));
            bool serialNumberHasCircledDay = Bomb.GetSerialNumberLetters().Any(sn => circledDayName.Name.Contains(sn));
            if (serialNumberHasActualDay == serialNumberHasCircledDay)
            {
                if (circledDay % 10 > 3)
                {
                    targetMonth = hasTargetSeason ? RepublicanMonth.Messidor : RepublicanMonth.Pluviose;
                }
                else
                {
                    targetMonth = hasTargetSeason ? RepublicanMonth.Ventose : RepublicanMonth.Floreal;
                }
            }
            else if (serialNumberHasActualDay)
            {
                if (circledDay % 2 == 0)
                {
                    targetMonth = hasTargetSeason ? RepublicanMonth.Frimaire : RepublicanMonth.Brumaire;
                }
                else
                {
                    targetMonth = hasTargetSeason ? RepublicanMonth.Prairial : RepublicanMonth.Vendemaire;
                }
            }
            else
            {
                if (new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 }.Contains(circledDay + 1))
                {
                    targetMonth = hasTargetSeason ? RepublicanMonth.Thermidor : RepublicanMonth.Fructidor;
                }
                else
                {
                    targetMonth = hasTargetSeason ? RepublicanMonth.Nivose : RepublicanMonth.Germinal;
                }
            }
        }
        else
        {
            bool serialNumberHasActualDay = Bomb.GetSerialNumberLetters().Any(sn => actualDayName.Namesake.Contains(sn));
            bool serialNumberHasCircledDay = Bomb.GetSerialNumberLetters().Any(sn => circledDayName.Namesake.Contains(sn));
            string circledMonthName = ((RepublicanMonth)circledMonth).ToString().ToUpper();

            bool digitOfWeekInSerialNumber = Bomb.GetSerialNumberNumbers().Contains(circledDay % 10);
            
            if(serialNumberHasActualDay == serialNumberHasCircledDay)
            {
                if (circledMonthName.Contains('N'))
                {
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Brumaire : RepublicanMonth.Prairial;
                }
                else
                {
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Frimaire : RepublicanMonth.Thermidor;
                }
            }
            else if (serialNumberHasActualDay)
            {
                if (circledMonthName.Contains('D'))
                {
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Messidor : RepublicanMonth.Floreal;
                }
                else
                {
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Pluviose : RepublicanMonth.Vendemaire;
                }
            }
            else
            {
                if (circledMonthName.Contains('V'))
                {
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Germinal : RepublicanMonth.Nivose;
                }
                else
                {
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Ventose : RepublicanMonth.Fructidor;
                }
            }
        }
        Log($"Target month is {targetMonth}");
    }

    void Log(string text) => Debug.LogFormat("[The French Republican Calendar #{0}] {1}", ModuleId, text);


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
