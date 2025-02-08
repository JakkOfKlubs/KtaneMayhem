using System;
using System.Collections;
using System.Collections.Generic;
using KModkit;
using Mayhem;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class MayhemScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] HexSelectables;
    public GameObject[] Hexes, HexFronts, HexBacks;
    public GameObject StatusLightObj, HexesParent;
    public Material HexBaseMat;
    public Light[] HexLights;

    public enum HexColor
    {
        Blue,
        Pink,
        Red,
        Black,
        White
    }

    private static readonly Color32[] _hexBlues = new Color32[19]
    {
        new Color32(00, 160, 210, 255),
        new Color32(00, 140, 210, 255),
        new Color32(00, 120, 210, 255),
        new Color32(20, 170, 220, 255),
        new Color32(20, 150, 220, 255),
        new Color32(20, 130, 220, 255),
        new Color32(20, 110, 220, 255),
        new Color32(40, 180, 230, 255),
        new Color32(40, 160, 230, 255),
        new Color32(40, 140, 230, 255),
        new Color32(40, 120, 230, 255),
        new Color32(40, 100, 230, 255),
        new Color32(60, 170, 240, 255),
        new Color32(60, 150, 240, 255),
        new Color32(60, 130, 240, 255),
        new Color32(60, 110, 240, 255),
        new Color32(80, 160, 250, 255),
        new Color32(80, 140, 250, 255),
        new Color32(80, 120, 250, 255)
    };

    private static readonly Color32[] _hexReds = new Color32[19]
    {
        new Color32(230, 30, 00, 255),
        new Color32(230, 20, 00, 255),
        new Color32(230, 10, 00, 255),
        new Color32(230, 35, 20, 255),
        new Color32(230, 25, 20, 255),
        new Color32(230, 15, 20, 255),
        new Color32(230, 05, 20, 255),
        new Color32(230, 40, 40, 255),
        new Color32(230, 30, 40, 255),
        new Color32(230, 20, 40, 255),
        new Color32(230, 10, 40, 255),
        new Color32(230, 00, 40, 255),
        new Color32(230, 35, 60, 255),
        new Color32(230, 25, 60, 255),
        new Color32(230, 15, 60, 255),
        new Color32(230, 05, 60, 255),
        new Color32(230, 30, 80, 255),
        new Color32(230, 20, 80, 255),
        new Color32(230, 10, 80, 255)
    };

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private int _startingHex;
    private int? _currentCorrectHex = null;
    private int? _highlightedHex = null;
    private readonly int[] _correctHexes = new int[7];
    private bool _areHexesRed;
    private bool _areHexesFlashing;
    private bool _canStagesContinue;
    private bool _areHexesBlack;
    private bool _trueModuleSolved;
    private static readonly string[] _soundNames = { "Flash1", "Flash2", "Flash3", "Flash4", "Flash5", "Flash6", "Flash7" };
    private static readonly string[] _ordinals = { "first", "second", "third", "fourth", "fifth", "sixth", "seventh" };
    private static readonly float[] _xPos = {
        -0.052f, -0.052f, -0.052f,
        -0.026f, -0.026f, -0.026f, -0.026f,
        0f, 0f, 0f, 0f, 0f,
        0.026f, 0.026f, 0.026f, 0.026f,
        0.052f, 0.052f, 0.052f };
    private static readonly float[] _zPos = {
        0.03f, 0f, -0.03f,
        0.045f, 0.015f, -0.015f, -0.045f,
        0.06f, 0.03f, 0f, -0.03f, -0.06f,
        0.045f, 0.015f, -0.015f, -0.045f,
        0.03f, 0f, -0.03f };
    private readonly HexColor[] _hexColors = new HexColor[19];
    private Coroutine _pulseLightCoroutine;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        float scalar = transform.lossyScale.x;
        foreach (Light light in HexLights)
            light.range *= scalar;
        for (int i = 0; i < HexSelectables.Length; i++)
        {
            HexSelectables[i].OnHighlight += HexHighlight(i);
            HexSelectables[i].OnHighlightEnded += HexHighlightEnd(i);
            HexSelectables[i].OnInteract += HexPress(i);
            HexSelectables[i].OnInteractEnded += HexRelease(i);
        }

        GenerateSolution();
        for (int hex = 0; hex < _hexColors.Length; hex++)
        {
            if (hex == _startingHex)
                SetHexColor(hex, HexColor.Pink);
            else
                SetHexColor(hex, HexColor.Blue);
        }
    }

    private void GenerateSolution()
    {
        _startingHex = Rnd.Range(0, 19);
        SetHexColor(_startingHex, HexColor.Pink);
        var sn = BombInfo.GetSerialNumber();
        int temp = 0;
        for (int i = 0; i < 7; i++)
        {
            if (i != 0)
                temp += sn[i - 1] >= '0' && sn[i - 1] <= '9' ? sn[i - 1] - '0' : sn[i - 1] - 'A' + 1;
            _correctHexes[i] = (_startingHex + temp) % 19;
        }
        Debug.LogFormat("[Mayhem #{0}] Starting hex is at position {1}.", _moduleId, _correctHexes[0] + 1);
        int tempTwo = _correctHexes[0] + 1;
        for (int i = 0; i < 6; i++)
        {
            if (tempTwo > 19)
                tempTwo -= 19;
            Debug.LogFormat("[Mayhem #{0}] The {1} character of the serial number is {2} ({3}). Adding this to {4} gets you the {5} hex at position {6}.",
                _moduleId, _ordinals[i], sn[i],
                sn[i] >= '0' && sn[i] <= '9' ? sn[i] - '0' : sn[i] - 'A' + 1,
                tempTwo, _ordinals[i + 1], _correctHexes[i + 1] + 1);
            tempTwo += sn[i] >= '0' && sn[i] <= '9' ? sn[i] - '0' : sn[i] - 'A' + 1;
        }
    }

    private void SetHexColor(int ix, HexColor color)
    {
        if (color != HexColor.White)
            _hexColors[ix] = color;
        if (ix == _highlightedHex && color != HexColor.White)
            return;
        switch (color)
        {
            case HexColor.Blue:
                HexFronts[ix].GetComponent<MeshRenderer>().material.color = _hexBlues[ix];
                HexBacks[ix].GetComponent<MeshRenderer>().material.color = _hexBlues[ix];
                break;
            case HexColor.Pink:
                HexFronts[ix].GetComponent<MeshRenderer>().material.color = new Color32(230, 50, 230, 255);
                HexBacks[ix].GetComponent<MeshRenderer>().material.color = new Color32(230, 50, 230, 255);
                break;
            case HexColor.Red:
                HexFronts[ix].GetComponent<MeshRenderer>().material.color = _hexReds[ix];
                HexBacks[ix].GetComponent<MeshRenderer>().material.color = _hexReds[ix];
                break;
            case HexColor.Black:
                HexFronts[ix].GetComponent<MeshRenderer>().material.color = new Color32(70, 70, 80, 255);
                HexBacks[ix].GetComponent<MeshRenderer>().material.color = new Color32(70, 70, 80, 255);
                break;
            case HexColor.White:
                HexFronts[ix].GetComponent<MeshRenderer>().material.color = new Color32(200, 200, 240, 255);
                HexBacks[ix].GetComponent<MeshRenderer>().material.color = new Color32(200, 200, 240, 255);
                break;
            default:
                break;
        }
    }

    private Action HexHighlight(int hex)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return;
            _highlightedHex = hex;
            SetHexColor(hex, HexColor.White);
        };
    }

    private Action HexHighlightEnd(int hex)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return;
            _highlightedHex = null;
            SetHexColor(hex, _hexColors[hex]);
        };
    }

    private KMSelectable.OnInteractHandler HexPress(int hex)
    {
        return delegate ()
        {
            if (_areHexesFlashing || _areHexesBlack || _moduleSolved)
                return false;
            HexHighlight(hex);
            _highlightedHex = hex;
            _canStagesContinue = true;
            StartCoroutine(FlashHexes());
            _areHexesFlashing = true;
            return false;
        };
    }

    private Action HexRelease(int hex)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return;
            HexHighlight(hex);
            _highlightedHex = hex;
        };
    }

    private IEnumerator FlashHexes()
    {
        yield return new WaitForSeconds(0.2f);
        for (int stage = 0; stage < _correctHexes.Length; stage++)
        {
            _currentCorrectHex = _correctHexes[stage];
            Audio.PlaySoundAtTransform(_soundNames[stage], transform);
            Pulse();
            yield return new WaitForSeconds(0.5f);
            Pulse();
            yield return new WaitForSeconds(0.5f);
            Pulse();
            yield return new WaitForSeconds(0.75f);
            Pulse();
            for (int hex = 0; hex < _hexColors.Length; hex++)
                if (stage == 0 && hex != _startingHex)
                    SetHexColor(hex, HexColor.Blue);
            for (int hex = 0; hex < _hexColors.Length; hex++)
                if (hex != _correctHexes[stage])
                    SetHexColor(hex, HexColor.Red);
            _areHexesRed = true;
            yield return new WaitForSeconds(2.12f);
            _areHexesRed = false;
            for (int hex = 0; hex < 19; hex++)
                SetHexColor(hex, HexColor.Blue);
            if (!_canStagesContinue)
            {
                StartCoroutine(OpenHex(_currentCorrectHex.Value, false));
                StartCoroutine(MoveLight(_currentCorrectHex.Value, false));
                yield break;
            }
            _currentCorrectHex = null;
            if (stage == 6)
            {
                Audio.PlaySoundAtTransform("Solve", transform);
                _areHexesFlashing = false;
                _moduleSolved = true;
                StartCoroutine(OpenHex(_correctHexes[6], true));
                StartCoroutine(MoveLight(_correctHexes[6], true));
            }
        }
    }

    private void Pulse()
    {
        if (_pulseLightCoroutine != null)
            StopCoroutine(_pulseLightCoroutine);
        _pulseLightCoroutine = StartCoroutine(PulseLightAnimation());
    }

    private IEnumerator PulseLightAnimation()
    {
        var duration = 0.5f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            for (int i = 0; i < HexLights.Length; i++)
                HexLights[i].intensity = Easing.OutSine(elapsed, 7f, 3f, duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int i = 0; i < HexLights.Length; i++)
            HexLights[i].intensity = 3f;
    }

    private IEnumerator OpenHex(int hex, bool isSolve)
    {
        var durationFirst = 0.3f;
        var elapsedFirst = 0f;
        float waitTime;
        if (isSolve)
            waitTime = 0.5f;
        else
            waitTime = 1f;
        Audio.PlaySoundAtTransform("HexOpen", transform);
        while (elapsedFirst < durationFirst)
        {
            Hexes[hex].transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsedFirst, 0f, 90f, durationFirst), 0f, 0f);
            yield return null;
            elapsedFirst += Time.deltaTime;
        }
        Hexes[hex].transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        yield return new WaitForSeconds(waitTime);
        var durationSecond = 0.3f;
        var elapsedSecond = 0f;
        while (elapsedSecond < durationSecond)
        {
            Hexes[hex].transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsedSecond, 90f, 0f, durationSecond), 0f, 0f);
            yield return null;
            elapsedSecond += Time.deltaTime;
        }
        Audio.PlaySoundAtTransform("HexClose", transform);
        Hexes[hex].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
    }

    private IEnumerator MoveLight(int pos, bool isSolve)
    {
        yield return new WaitForSeconds(0.5f);
        Audio.PlaySoundAtTransform("Hooo", transform);
        var durationFirst = 0.3f;
        var elapsedFirst = 0f;
        while (elapsedFirst < durationFirst)
        {
            StatusLightObj.transform.localPosition = new Vector3(_xPos[pos], Easing.InOutQuad(elapsedFirst, -0.04f, 0.05f, durationFirst), _zPos[pos]);
            yield return null;
            elapsedFirst += Time.deltaTime;
        }
        StatusLightObj.transform.localPosition = new Vector3(_xPos[pos], 0.05f, _zPos[pos]);
        float durationSecond = 0.2f;
        float waitTime = 0.1f;
        float yPos = 0.02f;
        var elapsedSecond = 0f;
        if (isSolve)
        {
            _highlightedHex = null;
            for (int i = 0; i < _hexColors.Length; i++)
                SetHexColor(i, HexColor.Blue);
            Module.HandlePass();
            _trueModuleSolved = true;
        }
        else
        {
            waitTime += 0.3f;
            durationSecond += 0.2f;
            yPos -= 0.06f;
            Module.HandleStrike();
        }
        yield return new WaitForSeconds(waitTime);
        while (elapsedSecond < durationSecond)
        {
            StatusLightObj.transform.localPosition = new Vector3(_xPos[pos], Easing.InOutQuad(elapsedSecond, 0.05f, yPos, durationSecond), _zPos[pos]);
            yield return null;
            elapsedSecond += Time.deltaTime;
        }
        StatusLightObj.transform.localPosition = new Vector3(_xPos[pos], yPos, _zPos[pos]);
        if (!_moduleSolved)
        {
            yield return new WaitForSeconds(0.2f);
            for (int hex = 0; hex < _hexColors.Length; hex++)
                SetHexColor(hex, HexColor.Black);
            _areHexesBlack = true;
            yield return new WaitForSeconds(1.0f);
            for (int hex = 0; hex < _hexColors.Length; hex++)
                SetHexColor(hex, HexColor.Blue);
            _areHexesBlack = false;
            GenerateSolution();
        }
        _areHexesFlashing = false;
    }

    private void Update()
    {
        if (_areHexesRed && _areHexesFlashing && _canStagesContinue && _currentCorrectHex != _highlightedHex)
        {
            _canStagesContinue = false;
            Debug.LogFormat("[Mayhem #{0}] The correct hex was not remained highlighted for entire duration of hexes being red. Strike.", _moduleId);
        }
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} U DR wait UL D wait (etc.) [activate module, wait, step in those directions, wait, step again etc.]";
#pragma warning restore 0414
    private static readonly Hex[] _hexes = new Hex[]
    {
        new Hex(-2, 0),
        new Hex(-2, 1),
        new Hex(-2, 2),
        new Hex(-1, -1),
        new Hex(-1, 0),
        new Hex(-1, 1),
        new Hex(-1, 2),
        new Hex(0, -2),
        new Hex(0, -1),
        new Hex(0, 0),
        new Hex(0, 1),
        new Hex(0, 2),
        new Hex(1, -2),
        new Hex(1, -1),
        new Hex(1, 0),
        new Hex(1, 1),
        new Hex(2, -2),
        new Hex(2, -1),
        new Hex(2, 0)
    };

    IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.ToLowerInvariant().Trim().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var elements = new int?[pieces.Length];
        var numWaits = 0;
        var curHex = _hexes[_startingHex];

        for (var i = 0; i < pieces.Length; i++)
        {
            switch (pieces[i])
            {
                case "ul": case "nw": case "10": curHex = curHex.GetNeighbor(0); break;
                case "u": case "n": case "12": curHex = curHex.GetNeighbor(1); break;
                case "ur": case "ne": case "2": curHex = curHex.GetNeighbor(2); break;
                case "dr": case "se": case "4": curHex = curHex.GetNeighbor(3); break;
                case "d": case "s": case "6": curHex = curHex.GetNeighbor(4); break;
                case "dl": case "sw": case "8": curHex = curHex.GetNeighbor(5); break;
                case "wait": case "w": case ".": elements[i] = null; numWaits++; continue;
                default: yield return string.Format("sendtochaterror Step #{0} is an invalid command: {1}", i + 1, pieces[i]); yield break;
            }
            if (curHex.Distance > 2)
            {
                yield return string.Format("sendtochaterror Step #{0} would move you out of bounds: {1}", i + 1, pieces[i]);
                yield break;
            }
            elements[i] = Array.IndexOf(_hexes, curHex);
        }

        if (numWaits != 5)
        {
            yield return "sendtochaterror I expected exactly 5 “wait”s.";
            yield break;
        }

        yield return null;
        yield return RunTPSequence(elements, isSolver: false);

        HexHighlightEnd(Array.IndexOf(_hexes, curHex));
        yield return "end multiple strikes";
        yield return "solve";
    }

    private IEnumerator RunTPSequence(IEnumerable<int?> elements, bool isSolver)
    {
        HexSelectables[_startingHex].OnInteract();
        HexSelectables[_startingHex].OnHighlight();
        while (!_areHexesRed)
            yield return null;
        while (_areHexesRed)
            yield return null;

        var prevHex = _startingHex;

        foreach (var tr in elements)
        {
            if (tr == null)
            {
                if (!isSolver)
                    yield return "multiple strikes";    // This tells TP not to abort the handler upon a strike
                while (!_areHexesRed)
                    yield return null;
                var mustAbort = !_canStagesContinue;
                while (_areHexesRed)
                    yield return null;
                if (mustAbort)
                {
                    HexSelectables[prevHex].OnHighlightEnded();
                    yield break;
                }
            }
            else
            {
                HexSelectables[prevHex].OnHighlightEnded();
                HexSelectables[tr.Value].OnHighlight();

                yield return new WaitForSeconds(isSolver ? .05f : .025f);
                prevHex = tr.Value;
            }
        }

        while (!_areHexesRed)
            yield return null;
        while (_areHexesRed)
            yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        var elements = new List<int?>();

        var curHex = _hexes[_startingHex];
        for (var i = 1; i < 7; i++)
        {
            var goal = _hexes[_correctHexes[i]];

            while (curHex != goal)
            {
                var diff = goal - curHex;
                int movement;
                if (diff.Q > 0 && diff.R < 0)
                    movement = 2;
                else if (diff.Q > 0)
                    movement = 3;
                else if (diff.Q < 0 && diff.R > 0)
                    movement = 5;
                else if (diff.Q < 0)
                    movement = 0;
                else if (diff.R > 0)
                    movement = 4;
                else
                    movement = 1;
                curHex = curHex.GetNeighbor(movement);
                elements.Add(Array.IndexOf(_hexes, curHex));
            }
            elements.Add(null);
        }
        elements.RemoveAt(elements.Count - 1);

        var e = RunTPSequence(elements, isSolver: true);
        while (e.MoveNext())
            yield return e.Current;

        while (!_trueModuleSolved)
            yield return true;
    }
}
