using Assets;
using KModkit;
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
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

    private RepublicanDayName actualDate;
    private RepublicanDayName circledDate;

    private int[] targetDays;
    private RepublicanMonth targetMonth;

    int displayedMonth = 0;
    static readonly string[] republicanMonths = {
        "Vendémiaire", "Brumaire", "Frimaire", "Nivôse", "Pluviôse", "Ventôse",
        "Germinal", "Floréal", "Prairial", "Messidor", "Thermidor", "Fructidor",
        "Sans-culottides"
    };

    static readonly string[] complementaryDays = {
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
    }

    private void PressDay(int i)
    {
        dayStructs[i - 1].selectable.AddInteractionPunch(.2f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (targetDays.Contains(i) && (int)targetMonth == displayedMonth)
        {
            Module.HandlePass();
            Log("Module solved.");
            ModuleSolved = true;
            Audio.PlaySoundAtTransform("SolveSound", transform);
        }
        else
        {
            Module.HandleStrike();
            Log($"Pressed day {i - 1}. Strike issued.");
            Audio.PlaySoundAtTransform("Snort", transform);
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
        RepublicanMonth month = (RepublicanMonth)Rnd.Range(0, republicanMonths.Length);
        int day = Rnd.Range(1, month == RepublicanMonth.SansCulottides ? 7 : 31);
        circledDate = Data.Get(day, month);
        Log($"Circled Date {circledDate}");
    }

    private void UpdateDisplay()
    {
        Month.text = republicanMonths[displayedMonth].ToUpper();
        dayStructs[circledDate.Day - 1].circle.enabled = displayedMonth == (int)circledDate.Month;
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

        actualDate = Data.Get(dayInMonth, (RepublicanMonth)monthIndex);
        Log($"French Republican Calendar Date: {actualDate}");
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
        GetCurrentRepublicanDay();
        int circledDayHumanIndexed;
        if (circledDate.Month == RepublicanMonth.SansCulottides)
        {
            switch (circledDate.Day)
            {
                case 1:
                case 4:
                    circledDayHumanIndexed = Bomb.GetModuleIDs().Count();
                    break;
                case 2:
                case 5:
                    circledDayHumanIndexed = Bomb.GetBatteryCount() + Bomb.GetIndicators().SelectMany(i => i.ToCharArray()).Where(c => !"AEIOU".Contains(c)).Count() + Bomb.GetPortCount();
                    break;
                case 3:
                case 6:
                    circledDayHumanIndexed = Bomb.GetSerialNumberNumbers().Sum();
                    break;
                default:
                    throw new ArgumentException(circledDate.Day.ToString());
            }
            circledDayHumanIndexed = (circledDayHumanIndexed % 30) + 1;
        }
        else
        {
            circledDayHumanIndexed = circledDate.Day;
        }

        Log($"Led color is {new string[] { "red", "yellow", "green", "blue" }[ledIndex]}");

        switch (ledIndex)
        {
            case 0: //red
                targetDays = new int[] { 10 * ((actualDate.Day - 1) / 10) + ((circledDayHumanIndexed - 1) % 10) + 1 };
                break;
            case 1: //yellow
                targetDays = new int[] { 10 * ((circledDayHumanIndexed - 1) / 10) + ((actualDate.Day - 1) % 10) + 1 };
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
        if (actualDate.Day % 2 == 1) //If the day of the week for the current date is odd
        {
            Log("Day of the week is odd");
            Season[] targetSeasons = new Season[] { Season.Autumn, Season.Winter };
            bool hasTargetSeason = targetSeasons.Contains(circledDate.GetSeason());

            bool serialNumberHasActualDay = Bomb.GetSerialNumberLetters().Any(sn => actualDate.Name.Contains(sn));
            bool serialNumberHasCircledDay = Bomb.GetSerialNumberLetters().Any(sn => circledDate.Name.Contains(sn));
            if (serialNumberHasActualDay == serialNumberHasCircledDay)
            {
                Log("Both or neither names match");
                if ((circledDate.Day - 1) % 10 > 3)
                {
                    Log("Circled day of the week >4");
                    targetMonth = hasTargetSeason ? RepublicanMonth.Messidor : RepublicanMonth.Pluviose;
                }
                else
                {
                    Log("Circled day of the week <5");
                    targetMonth = hasTargetSeason ? RepublicanMonth.Ventose : RepublicanMonth.Floreal;
                }
            }
            else if (serialNumberHasActualDay)
            {
                Log("Current day only matches");
                if (circledDate.Day % 2 == 1)
                {
                    Log("Circled day of the week odd");
                    targetMonth = hasTargetSeason ? RepublicanMonth.Frimaire : RepublicanMonth.Brumaire;
                }
                else
                {
                    Log("Circled day of the week even");
                    targetMonth = hasTargetSeason ? RepublicanMonth.Prairial : RepublicanMonth.Vendemaire;
                }
            }
            else
            {
                Log("Circled day only matches");
                if (new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 }.Contains(circledDate.Day))
                {
                    Log("Circled day of the week prime");
                    targetMonth = hasTargetSeason ? RepublicanMonth.Thermidor : RepublicanMonth.Fructidor;
                }
                else
                {
                    Log("Circled day of the week not prime");
                    targetMonth = hasTargetSeason ? RepublicanMonth.Nivose : RepublicanMonth.Germinal;
                }
            }
            Log($"Circled month is {(hasTargetSeason ? "" : "not")} in Winter or Autumn");
        }
        else
        {
            Log("Day of the week is even");
            bool serialNumberHasActualDay = Bomb.GetSerialNumberLetters().Any(sn => actualDate.Namesake.Contains(sn));
            bool serialNumberHasCircledDay = Bomb.GetSerialNumberLetters().Any(sn => circledDate.Namesake.Contains(sn));
            string circledMonthName = (circledDate.Month).ToString().ToUpper();

            bool digitOfWeekInSerialNumber = Bomb.GetSerialNumberNumbers().Contains(circledDate.Day % 10);

            if (serialNumberHasActualDay == serialNumberHasCircledDay)
            {
                Log("Both or neither namesakes match");
                if (circledMonthName.Contains('N'))
                {
                    Log("Month has N");
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Brumaire : RepublicanMonth.Prairial;
                }
                else
                {
                    Log("Month has no N");
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Frimaire : RepublicanMonth.Thermidor;
                }
            }
            else if (serialNumberHasActualDay)
            {
                Log("Current day only match");
                if (circledMonthName.Contains('D'))
                {
                    Log("Month has D");
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Messidor : RepublicanMonth.Floreal;
                }
                else
                {
                    Log("Month has no D");
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Pluviose : RepublicanMonth.Vendemaire;
                }
            }
            else
            {
                Log("Circled day only match");
                if (circledMonthName.Contains('V'))
                {
                    Log("Month has V");
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Germinal : RepublicanMonth.Nivose;
                }
                else
                {
                    Log("Month has no V");
                    targetMonth = digitOfWeekInSerialNumber ? RepublicanMonth.Ventose : RepublicanMonth.Fructidor;
                }
            }
            Log($"Circled day of the week is {(digitOfWeekInSerialNumber ? "" : "not")} in serial number");
        }
        Log($"Target month is {targetMonth}");
    }

    void Log(string text) => Debug.LogFormat("[The French Republican Calendar #{0}] {1}", ModuleId, text);


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} left/right/l/r # [Press the left/right arrow # times]. !{0} submit/s # | !{0} # [Press the # day]. !{0} holiday [Displays the month with the circled day].";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpper();
        bool isHolidayCommand = Regex.Match(command, @"^HOLIDAY$").Success;
        if (isHolidayCommand)
        {
            yield return null;
            while(displayedMonth != (int)circledDate.Month)
            {
                Arrows[1].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            yield break;
        }

        Match match = Regex.Match(command, @"^((LEFT|RIGHT|L|R|SUBMIT|S)\s+)?(\d+)$");
        if (!match.Success)
            yield break;

        int value;
        if (!int.TryParse(match.Groups[3].Value, out value) || value <= 0)
            yield break;
        string prefix = match.Groups[2].Value;

        bool isSubmit = new string[] { "SUBMIT", "S", "" }.Contains(prefix);

        if ((isSubmit &&
            ((displayedMonth == 12 && value > 6) || (displayedMonth != 12 && value > 30)))
            || (!isSubmit && value >= 13))
            yield break;

        yield return null;

        if (isSubmit)
        {
            dayStructs[value - 1].selectable.OnInteract();
            yield break;
        }

        int arrowIndex = 0;
        switch (prefix)
        {
            case "LEFT":
            case "L":
                arrowIndex = 0;
                break;
            case "RIGHT":
            case "R":
                arrowIndex = 1;
                break;
        }
        for (int i = 0; i < value; i++)
        {
            Arrows[arrowIndex].OnInteract();
            yield return new WaitForSeconds(.2f);
        }

    }

    IEnumerator TwitchHandleForcedSolve()
    {

        while (displayedMonth != (int)targetMonth)
        {
            Arrows[1].OnInteract();
            yield return new WaitForSeconds(.05f);
        }
        dayStructs[targetDays[0] - 1].selectable.OnInteract();
    }
}
