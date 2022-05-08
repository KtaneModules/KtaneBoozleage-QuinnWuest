using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class BoozleageScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] BoozleButtonSels;
    public GameObject[] BoozleButtonObjs;
    public GameObject[] BoozleButtonLetters;
    public Material[] BoozleButtonMats;

    public Texture[] BoozleSetOne;
    public Texture[] BoozleSetTwo;
    public Texture[] BoozleSetThree;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _actuallySolved;

    private int[] _buttonSets = new int[64];
    private int[] _buttonLetters = new int[64];
    private int[] _buttonColors = new int[64];
    private static readonly string[] _colorNames = { "RED", "YELLOW", "GREEN", "BLUE", "MAGENTA", "WHITE" };
    private static readonly int[][] _squares = new int[16][] {
        new int[4] {0, 7, 56, 63},
        new int[4] {1, 15, 48, 62},
        new int[4] {2, 23, 40, 61},
        new int[4] {3, 31, 32, 60},
        new int[4] {4, 24, 39, 59},
        new int[4] {5, 16, 47, 58},
        new int[4] {6, 8, 55, 57},
        new int[4] {9, 14, 49, 54},
        new int[4] {10, 22, 41, 53},
        new int[4] {11, 30, 33, 52},
        new int[4] {12, 25, 38, 51},
        new int[4] {13, 17, 46, 50},
        new int[4] {18, 21, 42, 45},
        new int[4] {19, 29, 34, 44},
        new int[4] {20, 26, 37, 43},
        new int[4] {27, 28, 35, 36}
    };
    private int[] _chosenSquares = new int[3];
    private int[][] _acceptablePos = new int[3][] { new int[4], new int[4], new int[4] };
    private bool[] _satisfied = new bool[3];
    private bool[] _animSatis = new bool[64];
    private int[] _spiralOrder = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 15, 23, 31, 39, 47, 55, 63, 62, 61, 60, 59, 58, 57, 56, 48, 40, 32, 24, 16, 8, 9, 10, 11, 12, 13, 14, 22, 30, 38, 46, 54, 53, 52, 51, 50, 49, 41, 33, 25, 17, 18, 19, 20, 21, 29, 37, 45, 44, 43, 42, 34, 26, 27, 28, 36, 35 };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < BoozleButtonSels.Length; i++)
            BoozleButtonSels[i].OnInteract += BoozleButtonPress(i);

        _chosenSquares = Enumerable.Range(0, 16).ToArray().Shuffle().Take(3).ToArray();
        var randomLetters = Enumerable.Range(0, 26).ToArray().Shuffle().Take(3).ToArray();
        for (int sq = 0; sq < _chosenSquares.Length; sq++)
        {
            for (int i = 0; i < _squares[_chosenSquares[sq]].Length; i++)
                _buttonLetters[_squares[_chosenSquares[sq]][i]] = randomLetters[sq];
        }
        for (int sq = 0; sq < _chosenSquares.Length; sq++)
        {
            for (int i = 0; i < _acceptablePos[sq].Length; i++)
                _acceptablePos[sq][i] = _squares[_chosenSquares[sq]][i];
            Debug.LogFormat("[Boozleage #{0}] Square {1} has letter {2} with vertices at {3}.", _moduleId, "ABC"[sq], "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_buttonLetters[_acceptablePos[sq][0]]], _acceptablePos[sq].Select(i => GetCoord(i)).Join(", "));
        }
        TryAgain:
        for (int i = 0; i < BoozleButtonObjs.Length; i++)
        {
            _buttonColors[i] = Rnd.Range(0, 6);
            _buttonSets[i] = Rnd.Range(0, 3);
            if (!_squares[_chosenSquares[0]].Contains(i) && !_squares[_chosenSquares[1]].Contains(i) && !_squares[_chosenSquares[2]].Contains(i))
                _buttonLetters[i] = Rnd.Range(0, 26);
        }
        for (int i = 0; i < _squares.Length; i++)
        {
            if (_chosenSquares.Contains(i))
                continue;
            if (_squares[i].Distinct().Count() == 1)
                goto TryAgain;
        }
        var ac = new List<int>();
        for (int i = 0; i < _acceptablePos.Length; i++)
        {
            var c = _acceptablePos[i].Select(j => _buttonColors[j]).ToArray();
            var s = _acceptablePos[i].Select(j => _buttonSets[j]).ToArray();
            ac.AddRange(c);
            if (s.Distinct().Count() != 3)
                goto TryAgain;
        }
        if (ac.Distinct().Count() < 5)
            goto TryAgain;
        Debug.LogFormat("[Boozleage #{0}] Full grid. Letter, then Boozleglyph Set, then Color:", _moduleId);
        for (int i = 0; i < BoozleButtonObjs.Length; i++)
        {
            BoozleButtonObjs[i].GetComponent<MeshRenderer>().material = BoozleButtonMats[_buttonColors[i]];
            if (_buttonSets[i] == 0)
                BoozleButtonLetters[i].GetComponent<MeshRenderer>().material.mainTexture = BoozleSetOne[_buttonLetters[i]];
            if (_buttonSets[i] == 1)
                BoozleButtonLetters[i].GetComponent<MeshRenderer>().material.mainTexture = BoozleSetTwo[_buttonLetters[i]];
            if (_buttonSets[i] == 2)
                BoozleButtonLetters[i].GetComponent<MeshRenderer>().material.mainTexture = BoozleSetThree[_buttonLetters[i]];
        }
        string str = "";
        for (int i = 0; i < 8; i++)
        {
            str = "";
            for (int j = 0; j < 8; j++)
            {
                str += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_buttonLetters[i * 8 + j]].ToString() + (_buttonSets[i * 8 + j] + 1).ToString() + _colorNames[_buttonColors[i * 8 + j]][0];
                if (j != 7)
                    str += " ";
            }
            Debug.LogFormat("[Boozleage #{0}] {1}", _moduleId, str);
        }
    }

    private string GetCoord(int num)
    {
        return "ABCDEFGH".Substring(num % 8, 1) + "12345678".Substring(num / 8, 1);
    }

    private KMSelectable.OnInteractHandler BoozleButtonPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, BoozleButtonSels[btn].transform);
            BoozleButtonSels[btn].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            if (!_acceptablePos[0].Contains(btn) && !_acceptablePos[1].Contains(btn) && !_acceptablePos[2].Contains(btn))
            {
                Module.HandleStrike();
                Debug.LogFormat("[Boozleage #{0}] Pressed {1}, which is not part of a square. Strike.", _moduleId, GetCoord(btn));
                return false;
            }
            for (int i = 0; i < 3; i++)
            {
                if (_acceptablePos[i].Contains(btn))
                {
                    if (_satisfied[i])
                    {
                        Module.HandleStrike();
                        Debug.LogFormat("[Boozleage #{0}] Pressed {1}, which has already been satisfied. Strike.", _moduleId, GetCoord(btn));
                    }
                }
            }
            var conds = CheckValidPress(btn, (int)BombInfo.GetTime() % 60);
            if (conds[0] == "false")
            {
                Module.HandleStrike();
                Debug.LogFormat("[Boozleage #{0}] The button at {1} was {2}, when it should have been {3}. Strike.", _moduleId, GetCoord(btn), conds[2], conds[1]);
                return false;
            }
            Debug.LogFormat("[Boozleage #{0}] The button at {1} was correctly {2}.", _moduleId, GetCoord(btn), conds[1]);
            for (int i = 0; i < 3; i++)
                if (_acceptablePos[i].Contains(btn))
                {
                    _satisfied[i] = true;
                    for (int j = 0; j < _acceptablePos[i].Length; j++)
                    {
                        BoozleButtonObjs[_acceptablePos[i][j]].GetComponent<MeshRenderer>().material = BoozleButtonMats[7];
                        BoozleButtonLetters[_acceptablePos[i][j]].SetActive(false);
                    }
                    if (_satisfied.Contains(false))
                        Audio.PlaySoundAtTransform("Correct", transform);
                }
            if (!_satisfied.Contains(false))
            {
                _moduleSolved = true;
                Debug.LogFormat("[Boozleage #{0}] The buttons of three different squares were correctly pressed. Module solved.", _moduleId);
                StartCoroutine(SolveAnimation());
            }
            return false;
        };
    }

    private string[] CheckValidPress(int btn, int time)
    {
        bool b = false;
        var num = (_buttonColors[btn] * 3) + _buttonSets[btn];
        var letter = _buttonLetters[btn] + 1;
        int val = 0;
        int tc = 0;
        int tcalc = 0;
        switch (num)
        {
            case 0:
                tc = 0;
                val = letter % 9 + 3;
                break;
            case 1:
                tc = 0;
                val = 11 - (letter % 9);
                break;
            case 2:
                tc = 1;
                val = letter % 10;
                break;
            case 3:
                tc = 1;
                val = 9 - (letter % 10);
                break;
            case 4:
                tc = 2;
                val = letter % 5;
                break;
            case 5:
                tc = 0;
                val = (2 * letter % 9) + 3;
                break;
            case 6:
                tc = 0;
                val = 11 - (2 * letter % 9);
                break;
            case 7:
                tc = 2;
                val = 2 * letter % 5;
                break;
            case 8:
                tc = 1;
                val = letter % 8;
                break;
            case 9:
                tc = 0;
                val = 11 - ((letter + 3) % 7);
                break;
            case 10:
                tc = 1;
                val = (letter + 2) % 10;
                break;
            case 11:
                tc = 2;
                val = 3 * letter % 5;
                break;
            case 12:
                tc = 1;
                val = (letter % 7) + 2;
                break;
            case 13:
                tc = 0;
                val = 12 - (letter % 10);
                break;
            case 14:
                tc = 0;
                val = (2 * letter % 11) + 2;
                break;
            case 15:
                tc = 0;
                val = 12 - (letter % 11);
                break;
            case 16:
                tc = 1;
                val = letter % 9;
                break;
            case 17:
                tc = 2;
                val = (letter % 3) + 3;
                break;
        }
        if (tc == 0)
            tcalc = (time / 10) + (time % 10);
        if (tc == 1)
            tcalc = time % 10;
        if (tc == 2)
            tcalc = Math.Abs((time / 10) - (time % 10));
        if (tcalc == val)
            b = true;
        return new[] { b ? "true" : "false",
            tc == 0 ? "pressed when the seconds digits summed to " + val :
            tc == 1 ? "pressed when the last digit of the timer was " + val:
            "pressed when the difference between the seconds digits were "+ val,
            tc == 0 ? "pressed when the seconds digits summed to " + tcalc :
            tc == 1 ? "pressed when the last digit of the timer was " + tcalc :
            "pressed when the difference between the seconds digits were "+ tcalc
        };
    }


    private IEnumerator SolveAnimation()
    {
        var rndShuff = Enumerable.Range(0, 6).ToArray().Shuffle();
        Audio.PlaySoundAtTransform("GrandStar", transform);
        for (int i = 0; i < 64; i++)
            BoozleButtonLetters[i].SetActive(false);
        for (int ix = 0; ix < 17; ix++)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (!_animSatis[r * 8 + c])
                        BoozleButtonObjs[r * 8 + c].GetComponent<MeshRenderer>().material = BoozleButtonMats[rndShuff[(ix + r + c) % 6]];
                }
            }
            if (ix == 16)
                StartCoroutine(SpiralTextures());
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator SpiralTextures()
    {
        for (int i = 0; i < 64; i++)
        {
            _animSatis[_spiralOrder[i]] = true;
            BoozleButtonObjs[63 - _spiralOrder[i]].GetComponent<MeshRenderer>().material = BoozleButtonMats[6];
            yield return new WaitForSeconds(0.044f);
        }
        for (int i = 0; i < 64; i++)
            BoozleButtonObjs[i].GetComponent<MeshRenderer>().material = BoozleButtonMats[2];
        Module.HandlePass();
        _actuallySolved = true;
        yield break;
    }

#pragma warning disable 0414
    private string TwitchHelpMessage = "!{0} press A1 at xx [Presses button A1 when the seconds digits are xx.] | !{0} press B2 at x [Presses button B2 when the last digit of the timer is x.] | Columns are labelled A-H. Rows are labelled 1-8.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*press\s+([A-H])([1-8])\s+at\s+(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        var pos = (int.Parse(m.Groups[2].Value) - 1) * 8 + (m.Groups[1].Value.ToUpperInvariant()[0] - 'A');
        string[] parameters = command.Split(' ');
        var timecmd = parameters[3];
        int timepress;
        if (!int.TryParse(timecmd, out timepress))
            yield break;
        if (timecmd.Length != 1 && timecmd.Length != 2)
            yield break;
        yield return null;
        if (timecmd.Length == 1)
        {
            while ((int)BombInfo.GetTime() % 10 != timepress)
                yield return "trycancel The button was not pressed due to a request to cancel";
        }
        else if (timecmd.Length == 2)
        {
            while ((int)BombInfo.GetTime() % 60 != timepress)
                yield return "trycancel The button was not pressed due to a request to cancel";
        }
        BoozleButtonSels[pos].OnInteract();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (_satisfied.Contains(false))
        {
            int time = (int)BombInfo.GetTime() % 60;
            for (int i = 0; i < _acceptablePos.Length; i++)
            {
                if (_satisfied[i])
                    continue;
                for (int j = 0; j < _acceptablePos[i].Length; j++)
                {
                    if (CheckValidPress(_acceptablePos[i][j], (int)BombInfo.GetTime() % 60)[0] == "true")
                    {
                        BoozleButtonSels[_acceptablePos[i][j]].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                    else
                        while (time == (int)BombInfo.GetTime() % 60)
                            yield return true;
                }
            }
            yield return null;
        }
        while (!_actuallySolved)
            yield return true;
        yield break;
    }
}
