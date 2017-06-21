﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Souvenir;
using UnityEngine;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Souvenir
/// Created by Timwi
/// </summary>
public class SouvenirModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable[] Answers4;
    public KMSelectable[] Answers6;
    public GameObject Answers4Parent;
    public GameObject Answers6Parent;

    public KMSelectable MainSelectable;
    public TextMesh TextMesh;
    public Renderer TextRenderer;
    public Renderer SurfaceRenderer;

    private static bool _isTimwisComputer = Environment.GetEnvironmentVariable("COMPUTERNAME") == "TEKELIA";
    private static string _timwiPath = @"D:\c\KTANE\Souvenir modules.txt";
    private Dictionary<string, List<QandA>> _questions = new Dictionary<string, List<QandA>>();
    private bool _isActivated = false;
    private bool _isInUnity = false;

    private QandA _currentQuestion = null;
    private bool _solveAfterCurrentQuestion = false;
    private bool _isSolved = false;
    private int _waitableModules;
    private double _surfaceSizeFactor;

    private Dictionary<string, int> _moduleCounts = new Dictionary<string, int>();
    private Dictionary<string, int> _modulesSolved = new Dictionary<string, int>();

    private List<int[]> _forgetMeNotDisplays = new List<int[]>();
    private List<int[]> _forgetMeNotSolutions = new List<int[]>();

    const string _Souvenir = "SouvenirModule(Clone)";

    const string _3DMaze = "3DMazeModule(Clone)";
    const string _AdventureGame = "AdventureGameModule(Clone)";
    const string _Bitmaps = "BitmapsModule(Clone)";
    const string _BrokenButtons = "BrokenButtonModule(Clone)";
    const string _CheapCheckout = "CheapCheckoutModule(Clone)";
    const string _Chess = "Chess(Clone)";
    const string _ColoredSquares = "ColoredSquaresModule(Clone)";
    const string _ConnectionCheck = "GraphModule(Clone)";
    const string _DoubleOh = "DoubleOhModule(Clone)";
    const string _FastMath = "fastMath(Clone)";
    const string _ForgetMeNot = "AdvancedMemory(Clone)";
    const string _Hexamaze = "HexamazeModule(Clone)";
    const string _Listening = "ListeningModule(Clone)";
    const string _MonsplodeFight = "CreatureModule(Clone)";
    const string _Morsematics = "AdvancedMorse(Clone)";
    const string _MouseInTheMaze = "Physics Module(Clone)";
    const string _Murder = "MurderModule(Clone)";
    const string _Neutralization = "neutralization(Clone)";
    const string _OnlyConnect = "OnlyConnectModule(Clone)";
    const string _OrientationCube = "OrientationModule(Clone)";
    const string _PerspectivePegs = "PerspectivePegsModule(Clone)";
    const string _SeaShells = "SeaShellsModule(Clone)";
    const string _SillySlots = "SillySlotsModule(Clone)";
    const string _SimonScreams = "SimonScreamsModule(Clone)";
    const string _SimonStates = "AdvancedSimon(Clone)";
    const string _SkewedSlots = "SkewedModule(Clone)";
    const string _TheBulb = "TheBulbModule(Clone)";
    const string _TwoBits = "TwoBitsModule(Clone)";

    private static int _SouvenirCounter = 1;
    private int _SouvenirID;

    private string[] _ignoreModules = new[] {
        // ────────────────────────────
        // 𝐍𝐨𝐭 𝐠𝐨𝐧𝐧𝐚 𝐝𝐨:
        // ────────────────────────────
        // Adjacent Letters
        "AdjacentLettersModule(Clone)",
        // Alphabet
        // Anagrams
        "Anagrams_Module(Clone)",
        // Astrology
        // Blind Alley
        // The Button
        // Caesar Cipher
        "CaesarCipherModule(Clone)",
        // Complicated Wires
        // Crazy Talk
        // Cryptography
        // Emoji Math
        "Emoji Math(Clone)",
        // English Test
        // Follow the Leader
        "FollowTheLeaderModule(Clone)",
        // Foreign Exchange Rates
        // Friendship
        // The Gamepad
        // Keypads
        // Laundry
        // Lettered Keys
        // Logic
        // Number Pad
        "NumberPadModule(Clone)",
        // Piano Keys
        // Plumbing
        // Probing
        // Resistors
        "ResistorsModule(Clone)",
        // Rock-Paper-Scissors-Lizard-Spock — RockPaperScissorsLizardSpockModule
        "RockPaperScissorsLizardSpockModule(Clone)",
        // Round Keypad
        // Safety Safe
        "AdvancedPassword(Clone)",
        // Square Button
        "AdvancedButton(Clone)",
        // Turn The Key
        // Turn The Keys
        // Wires
        // Word Scramble
        "Word_Scramble_Module(Clone)",


        // ────────────────────────────
        // 𝐂𝐚𝐧𝐝𝐢𝐝𝐚𝐭𝐞𝐬:
        // ────────────────────────────
        // Mazes
        // Memory
        // Microcontroller — Micro (first LED position only)
        "Micro(Clone)",
        // Morse Code
        // Mystic Square (position of skull and knight)
        // Passwords
        // Sea Shells — SeaShellsModule
        "SeaShellsModule(Clone)",
        // Shape Shift
        "ShapeShiftModule(Clone)",
        // Simon Says
        // Switches
        // Third Base
        // Tic-Tac-Toe — TicTacToeModule
        "TicTacToeModule(Clone)",
        // Who’s on First

        // ────────────────────────────
        // 𝐏𝐨𝐬𝐬𝐢𝐛𝐥𝐞 𝐟𝐮𝐭𝐮𝐫𝐞 𝐜𝐚𝐧𝐝𝐢𝐝𝐚𝐭𝐞𝐬:
        // ────────────────────────────
        // Color Flash
        // Combination Lock
        // Semaphore — SemaphoreModule
        "SemaphoreModule(Clone)",
        // Wire Sequences

        "dummy"
    };

    void setAnswerHandler(int index, Action<int> handler)
    {
        Answers6[index].OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Answers6[index].transform);
            Answers6[index].AddInteractionPunch();
            handler(index);
            return false;
        };
        Answers4[index].OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Answers4[index].transform);
            Answers4[index].AddInteractionPunch();
            handler(index);
            return false;
        };
    }

    void Start()
    {
        _SouvenirID = _SouvenirCounter;
        _SouvenirCounter++;

        Debug.LogFormat("[Souvenir #{0}] Started.", _SouvenirID);
        Bomb.OnBombExploded += delegate { StopAllCoroutines(); };
        Bomb.OnBombSolved += delegate { StopAllCoroutines(); };

        _attributes = typeof(Question).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => Ut.KeyValuePair((Question) f.GetValue(null), f.GetCustomAttribute<SouvenirQuestionAttribute>()))
            .Where(kvp => kvp.Value != null)
            .ToDictionary();

        if (transform.parent != null)
        {
            if (_isTimwisComputer)
                lock (_timwiPath)
                    File.WriteAllText(_timwiPath, "");

            _waitableModules = Bomb.GetSolvableModuleNames().Count;
            foreach (var module in Enumerable.Range(0, transform.parent.childCount)
                    .Select(i => transform.parent.GetChild(i).gameObject)
                    .Where(i => i.GetComponent<KMBombModule>() != null))
                StartCoroutine(ProcessModule(module));
        }

        var origRotation = SurfaceRenderer.transform.rotation;
        SurfaceRenderer.transform.eulerAngles = new Vector3(0, 180, 0);
        _surfaceSizeFactor = SurfaceRenderer.bounds.size.x / (2 * .834) * .9;
        SurfaceRenderer.transform.rotation = origRotation;

        disappear();
        SetWordWrappedText("Welcome to Souvenir");

        _isActivated = false;
        Module.OnActivate += delegate
        {
            _isActivated = true;
            if (Bomb.QueryWidgets("Unity", null).Count > 0)
            {
                // Testing in Unity
                Debug.LogFormat("[Souvenir #{0}] Entering Unity testing mode.", _SouvenirID);
                _isInUnity = true;
                var questions = Ut.GetEnumValues<Question>();
                var curQuestion = 0;
                var curOrd = 0;
                var curExample = 0;
                Action showQuestion = () =>
                {
                    SouvenirQuestionAttribute attr;
                    if (!_attributes.TryGetValue(questions[curQuestion], out attr))
                    {
                        Debug.LogFormat("[Souvenir #{1}] Error: Question {0} has no attribute.", questions[curQuestion], _SouvenirID);
                        return;
                    }
                    if (attr.ExampleExtraFormatArguments != null && attr.ExampleExtraFormatArguments.Length > 0 && attr.ExampleExtraFormatArgumentGroupSize > 0)
                    {
                        var numExamples = attr.ExampleExtraFormatArguments.Length / attr.ExampleExtraFormatArgumentGroupSize;
                        curExample = (curExample % numExamples + numExamples) % numExamples;
                    }
                    var fmt = new object[attr.ExampleExtraFormatArgumentGroupSize + 1];
                    fmt[0] = curOrd == 0 ? attr.AddThe ? "The\u00a0" + attr.ModuleName : attr.ModuleName : string.Format("the {0} you solved {1}", attr.ModuleName, ordinal(curOrd));
                    for (int i = 0; i < attr.ExampleExtraFormatArgumentGroupSize; i++)
                        fmt[i + 1] = attr.ExampleExtraFormatArguments[curExample * attr.ExampleExtraFormatArgumentGroupSize + i];
                    try
                    {
                        SetQuestion(new QandA(string.Format(attr.QuestionText, fmt), (attr.AllAnswers ?? attr.ExampleAnswers).ToList().Shuffle().Take(attr.NumAnswers).ToArray(), Rnd.Range(0, attr.NumAnswers), 0));
                    }
                    catch (FormatException e)
                    {
                        Debug.LogFormat("[Souvenir #{3}] FormatException {0}\nQuestionText={1}\nfmt=[{2}]", e.Message, attr.QuestionText, fmt.JoinString(", ", "\"", "\""), _SouvenirID);
                    }
                };
                showQuestion();

                setAnswerHandler(0, _ =>
                {
                    curQuestion = (curQuestion + questions.Length - 1) % questions.Length;
                    curExample = 0;
                    curOrd = 0;
                    showQuestion();
                });
                setAnswerHandler(1, _ =>
                {
                    curQuestion = (curQuestion + 1) % questions.Length;
                    curExample = 0;
                    curOrd = 0;
                    showQuestion();
                });
                setAnswerHandler(2, _ => { if (curOrd > 0) curOrd--; showQuestion(); });
                setAnswerHandler(3, _ => { curOrd++; showQuestion(); });
                setAnswerHandler(4, _ => { curExample--; showQuestion(); });
                setAnswerHandler(5, _ => { curExample++; showQuestion(); });
            }
            else
            {
                // Playing for real
                for (int i = 0; i < 6; i++)
                    setAnswerHandler(i, HandleAnswer);
                disappear();
                StartCoroutine(Play());
            }
        };

        Bomb.OnBombExploded += delegate { _exploded = true; };
    }

    private void disappear()
    {
        TextMesh.gameObject.SetActive(false);
        Answers4Parent.SetActive(false);
        Answers6Parent.SetActive(false);
    }

    private void HandleAnswer(int index)
    {
        if (_animating)
            return;

        if (_currentQuestion == null)
            return;

        if (_currentQuestion.CorrectIndex == index)
            dismissQuestion();
        else
        {
            Module.HandleStrike();
            if (!_exploded)
            {
                // Blink the correct answer, then move on to the next question
                _animating = true;
                StartCoroutine(revealThenMoveOn());
            }
        }
    }

    private void dismissQuestion()
    {
        _currentQuestion = null;
        disappear();
        if (_solveAfterCurrentQuestion)
        {
            _isSolved = true;
            Module.HandlePass();
        }
    }

    private bool _animating = false;
    private bool _exploded = false;

    private IEnumerator revealThenMoveOn()
    {
        yield return new WaitForSeconds(.3f);

        var on = false;
        var answ = Answers4Parent.activeSelf ? Answers4 : Answers6;
        var text = answ[_currentQuestion.CorrectIndex].GetComponent<TextMesh>().text;
        for (int i = 0; i < 15; i++)
        {
            answ[_currentQuestion.CorrectIndex].GetComponent<TextMesh>().text = on ? text : "";
            on = !on;
            yield return new WaitForSeconds(.1f);
        }
        yield return new WaitForSeconds(.3f);

        dismissQuestion();
        _animating = false;
    }

    private int _numQuestionsAsked = 0;
    private int _numQuestionsToAsk;
    private HashSet<int> _alreadyAskedAtNumSolved = new HashSet<int>();

    private IEnumerator Play()
    {
        // This relies on the fact that Play() isn’t started until the module is activated, while _waitableModules is decremented in ProcessModule() which is already started in Start().
        _numQuestionsToAsk = (_waitableModules + 1) / 2;
        Debug.LogFormat("[Souvenir #{1}] Entering KTANE mode. Striving to ask {0} questions.", _numQuestionsToAsk, _SouvenirID);

        while (true)
        {
            do
                yield return new WaitForSeconds(.5f);
            while (_currentQuestion != null);

            if (_isSolved)
                break;

            var solved = Bomb.GetSolvedModuleNames().Count;
            if (solved < _waitableModules)
            {
                // Don’t ask more than one question per solved module if there are still modules left to solve.
                if (_alreadyAskedAtNumSolved.Contains(solved))
                    continue;

                // Find a question to ask, or wait for a question to become available/eligible
                if (FindAndSetQuestion(solved))
                    _alreadyAskedAtNumSolved.Add(solved);
                continue;
            }

            // We ran out of modules to solve. Find a question that would otherwise be too soon to ask
            if (!FindAndSetQuestion(null))
            {
                // No questions to ask and no modules to solve.
                SetQuestion(new QandA("Congratulations!", new[] { "Thank you" }, 0, 0));
                _solveAfterCurrentQuestion = true;
                break;
            }
        }
    }

    private bool FindAndSetQuestion(int? unleashAt)
    {
        var eligible = _questions.Keys.Where(k => _questions[k].Any(q => unleashAt == null || q.UnleashAt <= unleashAt.Value));
        if (!eligible.Any())
            return false;
        var qMod = eligible.PickRandom();
        var qu = _questions[qMod].Where(q => unleashAt == null || q.UnleashAt <= unleashAt.Value).PickRandom();
        _questions[qMod].Remove(qu);
        if (_questions[qMod].Count == 0)
            _questions.Remove(qMod);
        SetQuestion(qu);
        _numQuestionsAsked++;
        if (_numQuestionsAsked >= _numQuestionsToAsk)
            _solveAfterCurrentQuestion = true;
        Debug.LogFormat("[Souvenir #{5}] Unleashing question (unleashAt={4}): {0}; questions asked={1}, questions to ask={2}, solveAfterCurrentQuestion={3}",
            qu.DebugString, _numQuestionsAsked, _numQuestionsToAsk, _solveAfterCurrentQuestion, unleashAt == null ? "null" : unleashAt.ToString(), _SouvenirID);
        return true;
    }

    private void SetQuestion(QandA q)
    {
        _currentQuestion = q;
        SetWordWrappedText(q.QuestionText);
        ShowAnswers(q.Answers);
    }

    private static double[][] _acceptableWidths = Ut.NewArray(
        // First value is y (vertical text advancement), second value is width of the Surface mesh at this y
        new[] { 0.834 - 0.834, 0.834 + 0.3556 },
        new[] { 0.834 - 0.7628, 0.834 + 0.424 },
        new[] { 0.834 - 0.6864, 0.834 + 0.424 },
        new[] { 0.834 - 0.528, 0.834 + 0.5102 },
        new[] { 0.834 - 0.4452, 0.834 + 0.6618 },
        new[] { 0.834 - 0.4452, 0.834 + 0.7745 },
        new[] { 0.834 - 0.391, 0.834 + 0.834 }
    );

    private void SetWordWrappedText(string text)
    {
        var low = 1;
        var high = 100;
        var desiredHeight = 1.1 * _surfaceSizeFactor;
        var wrappeds = new Dictionary<int, string>();
        var origRotation = TextMesh.transform.rotation;
        TextMesh.transform.eulerAngles = new Vector3(90, 0, 0);

        while (high - low > 1)
        {
            var mid = (low + high) / 2;
            TextMesh.fontSize = mid;

            TextMesh.text = "\u00a0";
            var size = TextRenderer.bounds.size;
            var widthOfASpace = size.x;
            var heightOfALine = size.z;
            var wrapWidths = new List<double>();

            var wrappedSB = new StringBuilder();
            var first = true;
            foreach (var line in Ut.WordWrap(
                text,
                line =>
                {
                    var y = line * heightOfALine / _surfaceSizeFactor;
                    if (line < wrapWidths.Count)
                        return wrapWidths[line];
                    while (wrapWidths.Count < line)
                        wrapWidths.Add(0);
                    var i = 1;
                    while (i < _acceptableWidths.Length && _acceptableWidths[i][0] < y)
                        i++;
                    if (i == _acceptableWidths.Length)
                        wrapWidths.Add(_acceptableWidths[i - 1][1] * _surfaceSizeFactor);
                    else
                    {
                        var lambda = (y - _acceptableWidths[i - 1][0]) / (_acceptableWidths[i][0] - _acceptableWidths[i - 1][0]);
                        wrapWidths.Add((_acceptableWidths[i - 1][1] * (1 - lambda) + _acceptableWidths[i][1] * lambda) * _surfaceSizeFactor);
                    }

                    return wrapWidths[line];
                },
                widthOfASpace,
                str =>
                {
                    TextMesh.text = str;
                    return TextRenderer.bounds.size.x;
                },
                allowBreakingWordsApart: false
            ))
            {
                if (line == null)
                {
                    // There was a word that was too long to fit into a line.
                    high = mid;
                    wrappedSB = null;
                    break;
                }
                if (!first)
                    wrappedSB.Append('\n');
                first = false;
                wrappedSB.Append(line);
            }

            if (wrappedSB != null)
            {
                var wrapped = wrappedSB.ToString();
                wrappeds[mid] = wrapped;
                TextMesh.text = wrapped;
                size = TextRenderer.bounds.size;
                if (size.z > desiredHeight)
                    high = mid;
                else
                    low = mid;
            }
        }

        TextMesh.fontSize = low;
        TextMesh.text = wrappeds[low];
        TextMesh.transform.rotation = origRotation;
        TextMesh.gameObject.SetActive(true);
    }

    void ShowAnswers(string[] answers)
    {
        if (answers == null || answers.Length == 0 || answers.Length > 6)
        {
            Debug.LogFormat("[Souvenir #{2}] Something went wrong setting answers. length={0}, answers=[{1}]", answers == null ? "null" : answers.Length.ToString(), answers == null ? "null" : answers.JoinString(), _SouvenirID);
            PassAndTurnOff("Error.");
            return;
        }

        var btns = answers.Length > 4 ? Answers6 : Answers4;

        Answers4Parent.SetActive(answers.Length <= 4);
        Answers6Parent.SetActive(answers.Length > 4);

        var children = new KMSelectable[6];
        for (int i = 0; i < btns.Length; i++)
        {
            var mesh = btns[i].GetComponent<TextMesh>();
            var renderer = btns[i].GetComponent<Renderer>();

            mesh.text = i < answers.Length ? answers[i] : "•";
            btns[i].gameObject.SetActive(_isInUnity || i < answers.Length);
            children[3 * (i % 2) + (i / 2)] = _isInUnity || i < answers.Length ? btns[i] : null;

            var highlight = btns[i].Highlight;

            var origRotation = mesh.transform.localRotation;
            mesh.transform.eulerAngles = new Vector3(90, 0, 0);
            mesh.transform.localScale = new Vector3(1, 1, 1);
            highlight.transform.localScale = new Vector3(100, 100, 100);
            var bounds = renderer.bounds.size;
            var fac = (answers.Length > 4 ? .45 : .7);
            if (bounds.x > fac * _surfaceSizeFactor)
            {
                // Adjust width of answer so that it fits horizontally
                mesh.transform.localScale = new Vector3((float) (fac * _surfaceSizeFactor / bounds.x), 1, 1);
                highlight.transform.localScale = new Vector3((float) (100 * bounds.x / (fac * _surfaceSizeFactor)), 100, 100);
            }
            mesh.transform.localRotation = origRotation;
        }

        MainSelectable.Children = children;
        MainSelectable.UpdateChildren();
    }

    private void PassAndTurnOff(string message = null)
    {
        Module.HandlePass();
        disappear();

        if (message != null)
        {
            TextMesh.gameObject.SetActive(true);
            SetWordWrappedText(message);
        }
    }

    sealed class FieldInfo<T>
    {
        private object _target;
        private int _souvenirID;
        public FieldInfo Field { get; private set; }

        public FieldInfo(object target, FieldInfo field, int souvenirID)
        {
            _target = target;
            Field = field;
            _souvenirID = souvenirID;
        }

        public T Get(bool nullAllowed = false)
        {
            var t = (T) Field.GetValue(_target);
            if (!nullAllowed && t == null)
                Debug.LogFormat("[Souvenir #{2}] {0} field {1} is null.", _target.GetType().FullName, Field.Name, _souvenirID);
            return t;
        }
        public void Set(T value) { Field.SetValue(_target, value); }
    }

    sealed class MethodInfo<T>
    {
        private object _target;
        public MethodInfo Method { get; private set; }

        public MethodInfo(object target, MethodInfo method)
        {
            _target = target;
            Method = method;
        }

        public T Invoke(params object[] arguments)
        {
            return (T) Method.Invoke(_target, arguments);
        }
    }

    private Component GetComponent(GameObject module, string name)
    {
        var comp = module.GetComponent(name);
        if (comp == null)
        {
            Debug.LogFormat("[Souvenir #{2}] {0} game object has no {1} component.", module.name, name, _SouvenirID);
            return null;
        }
        return comp;
    }

    private FieldInfo<T> GetField<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
        {
            Debug.LogFormat("[Souvenir #{3}] Attempt to get {1} field {0} of type {2} from a null object.", name, isPublic ? "public" : "non-public", typeof(T).FullName, _SouvenirID);
            return null;
        }
        var bindingFlags = (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | BindingFlags.Instance;
        var targetType = target.GetType();
        var fld = targetType.GetField(name, bindingFlags);
        if (fld == null)
        {
            Debug.LogFormat("[Souvenir #{3}] Type {0} does not contain {1} field {2}.", targetType, isPublic ? "public" : "non-public", name, _SouvenirID);
            return null;
        }
        if (!typeof(T).IsAssignableFrom(fld.FieldType))
        {
            Debug.LogFormat("[Souvenir #{5}] Type {0} has {1} field {2} of type {3} but expected type {4}.", targetType, isPublic ? "public" : "non-public", name, fld.FieldType.FullName, typeof(T).FullName, _SouvenirID);
            return null;
        }
        return new FieldInfo<T>(target, fld, _SouvenirID);
    }

    private MethodInfo<T> GetMethod<T>(object target, string name, int numParameters, bool isPublic = false)
    {
        if (target == null)
        {
            Debug.LogFormat("[Souvenir #{3}] Attempt to get {1} method {0} of return type {2} from a null object.", name, isPublic ? "public" : "non-public", typeof(T).FullName, _SouvenirID);
            return null;
        }
        var bindingFlags = (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | BindingFlags.Instance;
        var targetType = target.GetType();
        var mths = targetType.GetMethods(bindingFlags).Where(m => m.Name == name && m.GetParameters().Length == numParameters && typeof(T).IsAssignableFrom(m.ReturnType)).Take(2).ToArray();
        if (mths.Length == 0)
        {
            Debug.LogFormat("[Souvenir #{5}] Type {0} does not contain {1} method {2} with return type {3} and {4} parameters.", targetType, isPublic ? "public" : "non-public", name, typeof(T).FullName, numParameters, _SouvenirID);
            return null;
        }
        if (mths.Length > 1)
        {
            Debug.LogFormat("[Souvenir #{5}] Type {0} contains multiple {1} methods {2} with return type {3} and {4} parameters.", targetType, isPublic ? "public" : "non-public", name, typeof(T).FullName, numParameters, _SouvenirID);
            return null;
        }
        return new MethodInfo<T>(target, mths[0]);
    }

    private IEnumerator ProcessModule(GameObject module)
    {
        _moduleCounts.IncSafe(module.name);

        switch (module.name)
        {
            case _3DMaze:
                {
                    var comp = GetComponent(module, "ThreeDMazeModule");
                    var fldMap = GetField<object>(comp, "map");
                    var fldIsComplete = GetField<bool>(comp, "isComplete");
                    if (comp == null || fldMap == null || fldIsComplete == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var map = fldMap.Get();
                    if (map == null)
                        break;
                    var fldMapData = GetField<Array>(map, "mapData");
                    if (fldMapData == null)
                        break;
                    var mapData = fldMapData.Get();
                    if (mapData == null)
                        break;
                    if (mapData.GetLength(0) != 8 || mapData.GetLength(1) != 8)
                    {
                        Debug.LogFormat("[Souvenir #{2}] 3D maze wrong size ({0},{1}, expected 8,8).", mapData.GetLength(0), mapData.GetLength(1), _SouvenirID);
                        break;
                    }
                    var fldLabel = GetField<char>(mapData.GetValue(0, 0), "label", isPublic: true);
                    if (fldLabel == null)
                        break;
                    var chars = new HashSet<char>();
                    for (int i = 0; i < 8; i++)
                        for (int j = 0; j < 8; j++)
                        {
                            var ch = (char) fldLabel.Field.GetValue(mapData.GetValue(i, j));
                            if ("ABCDH".Contains(ch))
                                chars.Add(ch);
                        }
                    var correctMarkings = chars.OrderBy(c => c).JoinString();

                    char bearing;
                    if (correctMarkings == "ABC") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(1, 1));
                    else if (correctMarkings == "ABD") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(7, 0));
                    else if (correctMarkings == "ABH") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(0, 1));
                    else if (correctMarkings == "ACD") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(1, 2));
                    else if (correctMarkings == "ACH") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(0, 1));
                    else if (correctMarkings == "ADH") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(5, 0));
                    else if (correctMarkings == "BCD") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(6, 1));
                    else if (correctMarkings == "BCH") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(2, 2));
                    else if (correctMarkings == "BDH") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(3, 1));
                    else if (correctMarkings == "CDH") bearing = (char) fldLabel.Field.GetValue(mapData.GetValue(5, 1));
                    else
                    {
                        Debug.LogFormat(@"[Souvenir #{1}] Abandoning 3D Maze because unexpected markings: ""{0}"".", correctMarkings, _SouvenirID);
                        break;
                    }

                    if (!"NSWE".Contains(bearing))
                    {
                        Debug.LogFormat("[Souvenir #{1}] Abandoning 3D Maze because unexpected bearing: '{0}'.", bearing, _SouvenirID);
                        break;
                    }

                    while (!fldIsComplete.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_3DMaze);
                    addQuestion(Question._3DMazeMarkings, _3DMaze, new[] { correctMarkings });
                    addQuestion(Question._3DMazeBearing, _3DMaze, new[] { bearing == 'N' ? "North" : bearing == 'S' ? "South" : bearing == 'W' ? "West" : "East" });
                    break;
                }

            case _AdventureGame:
                {
                    var comp = GetComponent(module, "AdventureGameModule");
                    var fldButtonUse = GetField<KMSelectable>(comp, "ButtonUse", isPublic: true);
                    var fldStatValues = GetField<int[]>(comp, "StatValues");
                    var fldInvValues = GetField<IList>(comp, "InvValues"); // actually List<AdventureGameModule.ITEM>
                    var fldInvWeaponCount = GetField<int>(comp, "InvWeaponCount");
                    var fldSelectedItem = GetField<int>(comp, "SelectedItem");
                    var fldNumWeapons = GetField<int>(comp, "NumWeapons");
                    var mthItemName = GetMethod<string>(comp, "ItemName", 1);
                    var mthShouldUseItem = GetMethod<bool>(comp, "ShouldUseItem", 1);

                    if (comp == null || fldButtonUse == null || fldStatValues == null || fldInvValues == null || fldInvWeaponCount == null || fldSelectedItem == null || fldNumWeapons == null || mthItemName == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var statValues = fldStatValues.Get();
                    var invValues = fldInvValues.Get();
                    var buttonUse = fldButtonUse.Get();
                    if (statValues == null || invValues == null || buttonUse == null)
                        break;

                    var invWeaponCount = fldInvWeaponCount.Get();
                    var numWeapons = fldNumWeapons.Get();
                    if (invWeaponCount == 0 || numWeapons == 0)
                    {
                        Debug.LogFormat("[Souvenir #{2}] {0} field {1} is 0 (zero).", comp.GetType().FullName, invWeaponCount == 0 ? fldInvWeaponCount.Field.Name : fldNumWeapons.Field.Name, _SouvenirID);
                        break;
                    }

                    var prevInteract = buttonUse.OnInteract;
                    if (prevInteract == null)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Adventure Game: ButtonUse.OnInteract is null.", _SouvenirID);
                        break;
                    }

                    var origStatValues = statValues.ToArray();
                    var origInvValues = new List<int>(invValues.Cast<int>());
                    var correctItemsUsed = 0;
                    var qs = new List<Func<int, QandA>>();
                    var solved = false;

                    buttonUse.OnInteract = delegate
                    {
                        var selectedItem = fldSelectedItem.Get();
                        var itemUsed = origInvValues[selectedItem];
                        var shouldUse = mthShouldUseItem.Invoke(selectedItem);
                        for (int j = invWeaponCount; j < invValues.Count; j++)
                            shouldUse &= !mthShouldUseItem.Invoke(j);
                        var ret = prevInteract();

                        // If the stat values have changed, the user took a potion.
                        if (statValues[0] != origStatValues[0])
                            qs.Add(questionMaker(Question.AdventureGamePotion, _AdventureGame, new[] { origStatValues[0].ToString() }, new[] { "strength" }));
                        if (statValues[1] != origStatValues[1])
                            qs.Add(questionMaker(Question.AdventureGamePotion, _AdventureGame, new[] { origStatValues[1].ToString() }, new[] { "intelligence" }));
                        if (statValues[2] != origStatValues[2])
                            qs.Add(questionMaker(Question.AdventureGamePotion, _AdventureGame, new[] { origStatValues[2].ToString() }, new[] { "dexterity" }));
                        Array.Copy(statValues, origStatValues, statValues.Length);

                        if (invValues.Count != origInvValues.Count)
                        {
                            // If the length of the inventory has changed, the user used a correct non-weapon item.
                            correctItemsUsed++;
                            qs.Add(questionMaker(Question.AdventureGameCorrectItem, _AdventureGame, new[] { titleCase(mthItemName.Invoke(itemUsed)) }, new[] { ordinal(correctItemsUsed) }));
                            origInvValues.Clear();
                            origInvValues.AddRange(invValues.Cast<int>());
                        }
                        else if (shouldUse)
                        {
                            // The user solved the module.
                            solved = true;
                        }

                        return ret;
                    };

                    while (!solved)
                        yield return new WaitForSeconds(.1f);

                    buttonUse.OnInteract = prevInteract;
                    _modulesSolved.IncSafe(_AdventureGame);
                    foreach (var q in qs)
                        _questions.AddSafe(_AdventureGame, q(_modulesSolved.Get(_AdventureGame)));

                    break;
                }

            case _Bitmaps:
                {
                    var comp = GetComponent(module, "BitmapsModule");
                    var fldBitmap = GetField<bool[][]>(comp, "_bitmap");
                    var fldButtonToPush = GetField<int>(comp, "_buttonToPush");

                    if (comp == null || fldBitmap == null || fldButtonToPush == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    while (fldButtonToPush.Get() != 0)
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_Bitmaps);

                    var bitmap = fldBitmap.Get();
                    var qCounts = new int[4];
                    for (int x = 0; x < 8; x++)
                        for (int y = 0; y < 8; y++)
                            if (bitmap[x][y])
                                qCounts[(y / 4) * 2 + (x / 4)]++;

                    var preferredWrongAnswers = qCounts.SelectMany(i => new[] { i, 16 - i }).Distinct().Select(i => i.ToString()).ToArray();

                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { qCounts[0].ToString() }, new[] { "white", "top left" }, preferredWrongAnswers);
                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { qCounts[1].ToString() }, new[] { "white", "top right" }, preferredWrongAnswers);
                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { qCounts[2].ToString() }, new[] { "white", "bottom left" }, preferredWrongAnswers);
                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { qCounts[3].ToString() }, new[] { "white", "bottom right" }, preferredWrongAnswers);
                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { (16 - qCounts[0]).ToString() }, new[] { "black", "top left" }, preferredWrongAnswers);
                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { (16 - qCounts[1]).ToString() }, new[] { "black", "top right" }, preferredWrongAnswers);
                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { (16 - qCounts[2]).ToString() }, new[] { "black", "bottom left" }, preferredWrongAnswers);
                    addQuestion(Question.Bitmaps, _Bitmaps, new[] { (16 - qCounts[3]).ToString() }, new[] { "black", "bottom right" }, preferredWrongAnswers);

                    break;
                }

            case _ColoredSquares:
                {
                    var comp = GetComponent(module, "ColoredSquaresModule");
                    var fldExpectedPresses = GetField<object>(comp, "_expectedPresses");
                    var fldFirstStageColor = GetField<object>(comp, "_firstStageColor");

                    if (comp == null || fldExpectedPresses == null || fldFirstStageColor == null)
                        break;

                    yield return null;

                    // Colored Squares sets _expectedPresses to null when it’s solved
                    while (fldExpectedPresses.Get(nullAllowed: true) != null)
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_ColoredSquares);
                    addQuestion(Question.ColoredSquares, _ColoredSquares, new[] { fldFirstStageColor.Get().ToString() });
                    break;
                }

            case _ConnectionCheck:
                {
                    var comp = GetComponent(module, "GraphModule");
                    var fldOn = GetField<int[]>(comp, "On");
                    var fldCheckButton = GetField<KMSelectable>(comp, "Check", isPublic: true);
                    var fldDict = GetField<HashSet<Vector2>>(comp, "dict");
                    var fldQueries = GetField<Vector2[]>(comp, "Queries");

                    if (comp == null || fldOn == null || fldCheckButton == null || fldDict == null || fldQueries == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var isOn = fldOn.Get();
                    var checkButton = fldCheckButton.Get();
                    var dict = fldDict.Get();
                    var queries = fldQueries.Get();
                    if (isOn == null || checkButton == null || dict == null || queries == null)
                        break;

                    if (isOn.Length != 4 || isOn.Any(i => i < 0 || i > 1))
                    {
                        Debug.LogFormat("[Souvenir #{1}] Connection Check: Invalid value for ‘on’: [{0}]", isOn.JoinString(", "), _SouvenirID);
                        break;
                    }

                    var q = questionMaker(Question.ConnectionCheckInitial, _ConnectionCheck, new[] { isOn.Select(i => i == 0 ? "R" : "G").JoinString() });

                    var prevInteract = checkButton.OnInteract;
                    var completed = false;
                    checkButton.OnInteract = delegate
                    {
                        bool isSuccess = true;
                        for (int i = 0; i < 4; i++)
                            isSuccess &= dict.Contains(queries[i]) == (isOn[i] == 1);
                        if (isSuccess)
                            completed = true;
                        return prevInteract();
                    };

                    while (!completed)
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_ConnectionCheck);
                    _questions.AddSafe(_ConnectionCheck, q(_modulesSolved.Get(_ConnectionCheck)));
                    break;
                }

            case _DoubleOh:
                {
                    var comp = GetComponent(module, "DoubleOhModule");

                    var fldGrid = GetField<int[]>(comp, "_grid");
                    var fldCurPos = GetField<int>(comp, "_curPos");
                    var fldIsSolved = GetField<bool>(comp, "_isSolved");

                    if (comp == null || fldGrid == null || fldCurPos == null || fldIsSolved == null)
                        break;

                    yield return null;

                    var grid = fldGrid.Get();
                    var curPos = fldCurPos.Get();
                    if (grid == null)
                        break;
                    if (curPos < 0 || curPos >= 81)
                    {
                        Debug.LogFormat(@"[Souvenir #{0}] Double-Oh: invalid start position {1}.", _SouvenirID, curPos);
                        break;
                    }
                    if (grid.Length != 81 || grid[40] != 0)
                    {
                        Debug.LogFormat(@"[Souvenir #{0}] Double-Oh: grid has unexpected length ({1} instead of 81) or unexpected value at 40 ({2} instead of 0).", _SouvenirID, grid.Length, grid.Length != 81 ? "-" : grid[40].ToString());
                        break;
                    }

                    var value = grid[curPos];
                    while (!fldIsSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_DoubleOh);
                    addQuestion(Question.DoubleOh, _DoubleOh, new[] { value.ToString() });
                    break;
                }

            case _BrokenButtons:
                {
                    var comp = GetComponent(module, "BrokenButtonModule");
                    var fldPressed = GetField<List<string>>(comp, "Pressed");
                    var fldSolved = GetField<bool>(comp, "Solved");

                    if (comp == null || fldPressed == null || fldSolved == null)
                        break;

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    var pressed = fldPressed.Get();
                    if (pressed == null)
                        break;

                    _modulesSolved.IncSafe(_BrokenButtons);

                    for (int i = 0; i < pressed.Count; i++)
                        if (pressed[i].Length > 0) // skip the literally blank buttons.
                            addQuestion(Question.BrokenButtons, _BrokenButtons, new[] { pressed[i] }, new[] { ordinal(i + 1) }, pressed.Except(new[] { "" }).ToArray());

                    break;
                }

            case _CheapCheckout:
                {
                    var comp = GetComponent(module, "CheapCheckoutModule");
                    var fldPaid = GetField<decimal>(comp, "Paid");
                    var fldDisplay = GetField<decimal>(comp, "Display");
                    var fldWaiting = GetField<bool>(comp, "waiting");
                    var fldSolved = GetField<bool>(comp, "solved");

                    if (comp == null || fldPaid == null || fldDisplay == null || fldWaiting == null || fldSolved == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var paids = new List<decimal> { fldDisplay.Get() };
                    var paid = fldPaid.Get();
                    if (paid != paids[0])
                        paids.Add(paid);

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_CheapCheckout);

                    for (int i = 0; i < paids.Count; i++)
                        addQuestion(Question.CheapCheckoutPaid, _CheapCheckout, new[] { "$" + paids[i].ToString("N2") },
                            extraFormatArguments: new[] { paids.Count == 1 ? "" : ordinal(i + 1) + " " },
                            preferredWrongAnswers: Enumerable.Range(0, int.MaxValue).Select(_ => (decimal) Rnd.Range(5, 50)).Select(amt => "$" + amt.ToString("N2")).Distinct().Take(5).ToArray());

                    break;
                }

            case _Chess:
                {
                    var comp = GetComponent(module, "ChessBehaviour");
                    var fldIndexSelected = GetField<string[]>(comp, "indexSelected"); // this contains both the coordinates and the solution
                    var fldIsSolved = GetField<bool>(comp, "isSolved", isPublic: true);

                    if (comp == null || fldIndexSelected == null || fldIsSolved == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var indexSelected = fldIndexSelected.Get();
                    if (indexSelected == null)
                        break;
                    if (indexSelected.Length != 7 || indexSelected.Any(b => b == null || b.Length != 2))
                    {
                        Debug.LogFormat("[Souvenir #{1}] Abandoning Chess because indexSelected array length is unexpected or one of the values is null or not length 2 ({0}).", indexSelected.Select(iSel => iSel == null ? "null" : iSel).JoinString(", "), _SouvenirID);
                        break;
                    }

                    while (!fldIsSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_Chess);

                    for (int i = 0; i < 6; i++)
                        addQuestion(Question.ChessCoordinate, _Chess, new[] { indexSelected[i] }, new[] { ordinal(i + 1) });
                    break;
                }

            case _FastMath:
                {
                    var comp = GetComponent(module, "fastMath");
                    var fldScreen = GetField<TextMesh>(comp, "Screen", isPublic: true);
                    var fldSolved = GetField<bool>(comp, "_isSolved");

                    if (comp == null || fldScreen == null || fldSolved == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var prevLetters = new HashSet<string>();
                    string letters = null;
                    while (!fldSolved.Get())
                    {
                        var display = fldScreen.Get().text;
                        if (display.Length != 3)
                        {
                            Debug.LogFormat(@"[Souvenir #{1}] Abandoning Fast Math because the screen contains something other than three characters: ""{0}"" ({2} characters).", display, _SouvenirID, display.Length);
                            goto abandon;
                        }
                        letters = display[0] + "" + display[2];
                        prevLetters.Add(letters);
                        yield return new WaitForSeconds(.1f);
                    }
                    if (letters == null)
                    {
                        Debug.LogFormat(@"[Souvenir #{0}] Abandoning Fast Math because no letters were extracted before the module was solved.", _SouvenirID);
                        goto abandon;
                    }

                    _modulesSolved.IncSafe(_FastMath);
                    addQuestion(Question.FastMathLastLetters, _FastMath, new[] { letters }, preferredWrongAnswers: prevLetters.ToArray());

                    abandon:;
                    break;
                }

            case _ForgetMeNot:
                {
                    _waitableModules--;

                    var fmnIndex = _forgetMeNotDisplays.Count;
                    _forgetMeNotDisplays.Add(null);
                    _forgetMeNotSolutions.Add(null);

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var comp = GetComponent(module, "AdvancedMemory");
                    var fldDisplay = GetField<int[]>(comp, "Display");
                    var fldSolution = GetField<int[]>(comp, "Solution");

                    if (comp == null || fldDisplay == null || fldSolution == null)
                        break;

                    var display = fldDisplay.Get();
                    var solution = fldSolution.Get();

                    if (display == null || solution == null)
                        break;

                    _forgetMeNotDisplays[fmnIndex] = display;
                    _forgetMeNotSolutions[fmnIndex] = solution;

                    yield return new WaitForSeconds(.5f);
                    Debug.LogFormat("[Souvenir #{3}] Forget Me Not displays=[{0}], solutions=[{1}], I’m #{2}.",
                        _forgetMeNotDisplays.Select(arr => arr == null ? "null" : "[" + arr.JoinString(", ") + "]").JoinString("; "),
                        _forgetMeNotSolutions.Select(arr => arr == null ? "null" : "[" + arr.JoinString(", ") + "]").JoinString("; "),
                        fmnIndex, _SouvenirID);

                    int pos;
                    if ((pos = _forgetMeNotDisplays.IndexOf(null)) != -1 || (pos = _forgetMeNotSolutions.IndexOf(null)) != -1)
                    {
                        Debug.LogFormat("[Souvenir #{1}] Abandoning Forget Me Not because there’s an uninitialized one at index #{0}.", pos, _SouvenirID);
                        break;
                    }
                    if (display.Length != solution.Length || _forgetMeNotDisplays.Any(d => d.Length != solution.Length) || _forgetMeNotSolutions.Any(s => s.Length != solution.Length))
                    {
                        Debug.LogFormat("[Souvenir #{0}] Abandoning Forget Me Not because the arrays have inconsistent lengths.", _SouvenirID);
                        break;
                    }

                    var firstUniqueN = Enumerable.Range(0, display.Length).Select(i => (int?) i).FirstOrDefault(i => _forgetMeNotDisplays.All(arr => arr == display || arr[i.Value] != display[i.Value]));
                    if (firstUniqueN == null)
                    {
                        Debug.LogFormat("[Souvenir #{1}] Abandoning Forget Me Not because there is no index at which this one (#{0})’s display number is unique.", fmnIndex, _SouvenirID);
                        break;
                    }
                    var firstUnique = firstUniqueN.Value;

                    for (int i = 0; i < _waitableModules; i++)
                    {
                        if (i != firstUnique)
                            addQuestion(Question.ForgetMeNot, _ForgetMeNot, new[] { display[i].ToString() },
                                new[] { ordinal(i + 1), _moduleCounts.Get(_ForgetMeNot) == 1 ? "Forget Me Not" : string.Format("the Forget Me Not whose {0}-stage displayed number was {1}", ordinal(firstUnique + 1), display[firstUnique]) }, unleashAt: i + 2);
                    }

                    break;
                }

            case _Hexamaze:
                {
                    var comp = GetComponent(module, "HexamazeModule");
                    var fldPawnColor = GetField<int>(comp, "_pawnColor");
                    var fldSolved = GetField<bool>(comp, "_isSolved");
                    if (comp == null | fldPawnColor == null || fldSolved == null)
                        break;

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_Hexamaze);
                    var pawnColor = fldPawnColor.Get();
                    if (pawnColor < 0 || pawnColor >= 6)
                    {
                        Debug.LogFormat("[Souvenir #{1}] Abandoning Hexamaze because invalid pawn color {0}.", pawnColor, _SouvenirID);
                        break;
                    }

                    addQuestion(Question.HexamazePawnColor, _Hexamaze, new[] { new[] { "Red", "Yellow", "Green", "Cyan", "Blue", "Pink" }[pawnColor] });
                    break;
                }

            case _Listening:
                {
                    var comp = GetComponent(module, "Listening");
                    var fldIsActivated = GetField<bool>(comp, "isActivated");
                    var fldCodeInput = GetField<char[]>(comp, "codeInput");
                    var fldCodeInputPosition = GetField<int>(comp, "codeInputPosition");
                    var fldSound = GetField<object>(comp, "sound");
                    var fldDollarButton = GetField<KMSelectable>(comp, "DollarButton", isPublic: true);
                    var fldPoundButton = GetField<KMSelectable>(comp, "PoundButton", isPublic: true);
                    var fldStarButton = GetField<KMSelectable>(comp, "StarButton", isPublic: true);
                    var fldAmpersandButton = GetField<KMSelectable>(comp, "AmpersandButton", isPublic: true);

                    if (comp == null || fldIsActivated == null || fldCodeInput == null || fldCodeInputPosition == null || fldSound == null || fldDollarButton == null || fldPoundButton == null || fldStarButton == null || fldAmpersandButton == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var attr = _attributes.Get(Question.Listening);
                    if (attr == null)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Abandoning Listening because SouvenirQuestionAttribute for Question.Listening is null.", _SouvenirID);
                        break;
                    }

                    var sound = fldSound.Get();
                    var buttons = new[] { fldDollarButton.Get(), fldPoundButton.Get(), fldStarButton.Get(), fldAmpersandButton.Get() };
                    if (sound == null || buttons.Contains(null))
                        break;

                    var prevInteracts = buttons.Select(btn => btn.OnInteract).ToArray();
                    var nullIndex = Array.IndexOf(prevInteracts, null);
                    if (nullIndex != -1)
                    {
                        Debug.LogFormat("[Souvenir #{1}] Abandoning Listening because buttons[{0}].OnInteract is null.", nullIndex, _SouvenirID);
                        break;
                    }

                    var fldSoundCode = GetField<string>(sound, "code", isPublic: true);
                    if (fldSoundCode == null)
                        break;
                    var correctCode = fldSoundCode.Get();
                    if (correctCode == null)
                        break;

                    var code = "";
                    var solved = false;
                    for (int i = 0; i < 4; i++)
                    {
                        // Workaround bug in Mono 2.0 C# compiler
                        new Action<int>(j =>
                        {
                            buttons[i].OnInteract = delegate
                            {
                                var ret = prevInteracts[j]();
                                code += "$#*&"[j];
                                if (code.Length == 5)
                                {
                                    if (code == correctCode)
                                    {
                                        solved = true;
                                        // Sneaky: make it so that the player can no longer play the sound
                                        fldIsActivated.Set(false);
                                    }
                                    code = "";
                                }
                                return ret;
                            };
                        })(i);
                    }

                    while (!solved)
                        yield return new WaitForSeconds(.1f);

                    for (int i = 0; i < 4; i++)
                        buttons[i].OnInteract = prevInteracts[i];

                    _modulesSolved.IncSafe(_Listening);
                    addQuestion(Question.Listening, _Listening, new[] { correctCode }, preferredWrongAnswers: attr.ExampleAnswers);

                    break;
                }

            case _MonsplodeFight:
                {
                    var comp = GetComponent(module, "MonsplodeFightModule");
                    var fldCreatureData = GetField<object>(comp, "CD", isPublic: true);
                    var fldMovesData = GetField<object>(comp, "MD", isPublic: true);
                    var fldCreatureID = GetField<int>(comp, "crID");
                    var fldMoveIDs = GetField<int[]>(comp, "moveIDs");
                    var fldRevive = GetField<bool>(comp, "revive");
                    var fldButtons = GetField<KMSelectable[]>(comp, "buttons", isPublic: true);
                    var fldCorrectCount = GetField<int>(comp, "correctCount");

                    if (comp == null || fldCreatureData == null || fldMovesData == null || fldCreatureID == null || fldMoveIDs == null || fldRevive == null || fldButtons == null || fldCorrectCount == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var creatureData = fldCreatureData.Get();
                    var movesData = fldMovesData.Get();
                    var buttons = fldButtons.Get();
                    if (creatureData == null || movesData == null || buttons == null)
                        break;
                    var buttonNullIndex = Array.IndexOf(buttons, null);
                    if (buttons.Length != 4 || buttonNullIndex != -1)
                    {
                        Debug.LogFormat("[Souvenir #{2}] Abandoning Monsplode, Fight! because unexpected buttons array length ({0}, expected 4) or one of them is null ({1}, expected -1).", buttons.Length, buttonNullIndex, _SouvenirID);
                        break;
                    }

                    var fldCreatureNames = GetField<string[]>(creatureData, "names", isPublic: true);
                    var fldMoveNames = GetField<string[]>(movesData, "names", isPublic: true);
                    if (fldCreatureNames == null || fldMoveNames == null)
                        break;

                    var creatureNames = fldCreatureNames.Get();
                    var moveNames = fldMoveNames.Get();
                    if (creatureNames == null || moveNames == null)
                        break;

                    var displayedCreature = new List<string>();
                    var displayedMoves = new List<string[]>();
                    var pushedMoves = new List<int>();
                    var correctMoves = new List<bool>();
                    var finished = false;

                    var origInteracts = buttons.Select(btn => btn.OnInteract).ToArray();
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        // Need an extra scope to work around bug in Mono 2.0 C# compiler
                        new Action<int>(j =>
                        {
                            buttons[j].OnInteract = delegate
                            {
                                // Before processing the button push, get the creature and moves
                                string curCreatureName = null;
                                string[] curMoveNames = null;

                                var creatureID = fldCreatureID.Get();
                                if (creatureID < 0 || creatureID >= creatureNames.Length || string.IsNullOrEmpty(creatureNames[creatureID]))
                                    Debug.LogFormat("[Souvenir #{2}] Monsplode, Fight!: Unexpected creature ID: {0}; creature names are: [{1}]", creatureID, creatureNames.Select(cn => cn == null ? "null" : '"' + cn + '"').JoinString(", "), _SouvenirID);
                                else
                                {
                                    var moveIDs = fldMoveIDs.Get();
                                    if (moveIDs == null || moveIDs.Length != 4 || moveIDs.Any(mid => mid >= moveNames.Length || string.IsNullOrEmpty(moveNames[mid])))
                                        Debug.LogFormat("[Souvenir #{2}] Monsplode, Fight!: Unexpected move IDs: {0}; moves names are: [{1}]",
                                            moveIDs == null ? null : "[" + moveIDs.JoinString(", ") + "]",
                                            moveNames.Select(mn => mn == null ? "null" : '"' + mn + '"').JoinString(", "),
                                            _SouvenirID);
                                    else
                                    {
                                        curCreatureName = creatureNames[creatureID];
                                        curMoveNames = moveIDs.Select(mid => moveNames[mid].Replace("\r", "").Replace("\n", " ")).ToArray();
                                    }
                                }

                                var prevCorrectCount = fldCorrectCount.Get();
                                var ret = origInteracts[j]();

                                if (curCreatureName == null || curMoveNames == null)
                                {
                                    Debug.LogFormat("[Souvenir #{0}] Monsplode, Fight!: Abandoning due to error above.", _SouvenirID);
                                    // Set these to null to signal that something went wrong and we need to abort
                                    displayedCreature = null;
                                    displayedMoves = null;
                                    pushedMoves = null;
                                    correctMoves = null;
                                    finished = true;
                                }
                                else
                                {
                                    var wasCorrect = fldCorrectCount.Get() > prevCorrectCount;
                                    // If ‘revive’ is ‘false’, there is not going to be another stage.
                                    if (!fldRevive.Get())
                                        finished = true;

                                    if (curCreatureName != null && curMoveNames != null && displayedCreature != null && displayedMoves != null)
                                    {
                                        displayedCreature.Add(curCreatureName);
                                        displayedMoves.Add(curMoveNames);
                                        pushedMoves.Add(j);
                                        correctMoves.Add(wasCorrect);
                                    }
                                }
                                return ret;
                            };
                        })(i);
                    }

                    while (!finished)
                        yield return new WaitForSeconds(.1f);
                    _modulesSolved.IncSafe(_MonsplodeFight);

                    for (int i = 0; i < buttons.Length; i++)
                        buttons[i].OnInteract = origInteracts[i];

                    if (displayedCreature == null || displayedMoves == null)
                        break;

                    if (displayedCreature.Count != displayedMoves.Count || displayedCreature.Count != pushedMoves.Count || displayedCreature.Count != correctMoves.Count)
                    {
                        Debug.LogFormat("[Souvenir #{4}] Monsplode, Fight!: Inconsistent list lengths: {0}, {1}, {2}, {3}.", displayedCreature.Count, displayedMoves.Count, pushedMoves.Count, correctMoves.Count, _SouvenirID);
                        break;
                    }

                    var attr = _attributes.Get(Question.MonsplodeFightMove);
                    var allDisplayedCreatures = displayedCreature.ToArray();
                    var allDisplayedMoves = displayedMoves.SelectMany(x => x).Distinct().ToArray();
                    for (int i = 0; i < displayedCreature.Count; i++)
                    {
                        addQuestion(Question.MonsplodeFightCreature, _MonsplodeFight, new[] { displayedCreature[i] }, new[] { displayedCreature.Count == 1 ? "" : ordinal(i + 1) + " " }, allDisplayedCreatures);
                        var ord = displayedCreature.Count == 1 ? "" : string.Format("for the {0} creature ", ordinal(i + 1));
                        addQuestion(Question.MonsplodeFightMove, _MonsplodeFight, displayedMoves[i], new[] { "was", ord }, allDisplayedMoves);
                        if (attr != null && attr.AllAnswers != null)
                            addQuestion(Question.MonsplodeFightMove, _MonsplodeFight, attr.AllAnswers.Except(displayedMoves[i]).ToArray(), new[] { "was not", ord }, allDisplayedMoves);
                    }

                    break;
                }

            case _Morsematics:
                {
                    var comp = GetComponent(module, "AdvancedMorse");
                    var fldSolved = GetField<bool>(comp, "solved");
                    var fldChars = GetField<string[]>(comp, "DisplayCharsRaw");

                    if (comp == null || fldSolved == null)
                        break;

                    yield return null;

                    var chars = fldChars.Get();
                    if (chars == null)
                        break;
                    if (chars.Length != 3)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Morsematics: Unexpected length of DisplayCharsRaw array ({1} instead of 3).", _SouvenirID, chars.Length);
                        break;
                    }

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_Morsematics);
                    for (int i = 0; i < 3; i++)
                        addQuestion(Question.MorsematicsReceivedLetters, _Morsematics, new[] { chars[i] }, new[] { ordinal(i + 1) }, chars);
                    break;
                }

            case _MouseInTheMaze:
                {
                    var comp = GetComponent(module, "Maze_3d");
                    var fldObjectives = GetField<int[]>(comp, "objectives");
                    var fldIsActive = GetField<bool>(comp, "isActive");

                    if (comp == null || fldObjectives == null || fldIsActive == null)
                        break;

                    var objectives = fldObjectives.Get();
                    if (objectives == null)
                        break;
                    if (objectives.Length != 6)
                    {
                        Debug.LogFormat("[Souvenir #{1}] Mouse in the Maze: Objectives array has unexpected length ({0}; expected 6).", objectives.Length, _SouvenirID);
                        break;
                    }

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var torusColor = objectives[4];
                    var goalPos = objectives[5];
                    var goalColor = objectives[goalPos];

                    if (torusColor < 0 || torusColor >= 4 || goalColor < 0 || goalColor >= 4)
                    {
                        Debug.LogFormat("[Souvenir #{2}] Mouse in the Maze: Unexpected color (torus={0}; goal={1}).", torusColor, goalColor, _SouvenirID);
                        break;
                    }

                    while (fldIsActive.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_MouseInTheMaze);
                    addQuestion(Question.MouseInTheMazeSphere, _MouseInTheMaze, new[] { new[] { "white", "green", "blue", "yellow" }[goalColor] });
                    addQuestion(Question.MouseInTheMazeTorus, _MouseInTheMaze, new[] { new[] { "white", "green", "blue", "yellow" }[torusColor] });

                    break;
                }

            case _Murder:
                {
                    var comp = GetComponent(module, "MurderModule");
                    var fldSolved = GetField<bool>(comp, "isSolved");
                    var fldSolution = GetField<int[]>(comp, "solution");
                    var fldNames = GetField<string[,]>(comp, "names");
                    var fldSkipDisplay = GetField<int[,]>(comp, "skipDisplay");
                    var fldSuspects = GetField<int>(comp, "suspects");
                    var fldWeapons = GetField<int>(comp, "weapons");
                    var fldBodyFound = GetField<int>(comp, "bodyFound");

                    if (comp == null || fldSolved == null || fldNames == null || fldSkipDisplay == null || fldSuspects == null || fldWeapons == null || fldBodyFound == null)
                        break;

                    yield return null;

                    if (fldSuspects.Get() != 4 || fldWeapons.Get() != 4)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Murder: Unexpected number of suspects ({1} instead of 4) or weapons ({2} instead of 4).", _SouvenirID, fldSuspects.Get(), fldWeapons.Get());
                        break;
                    }

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);
                    _modulesSolved.IncSafe(_Murder);

                    var solution = fldSolution.Get();
                    var skipDisplay = fldSkipDisplay.Get();
                    var names = fldNames.Get();
                    if (solution == null || skipDisplay == null || names == null)
                        break;
                    if (solution.Length != 3 || skipDisplay.GetLength(0) != 2 || skipDisplay.GetLength(1) != 6 || names.GetLength(0) != 3 || names.GetLength(1) != 9)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Murder: Unexpected length of solution array ({1} instead of 3) or solution array ({2}/{3} instead of 2/6) or names array ({4}/{5} instead of 3/9).", _SouvenirID, solution.Length, skipDisplay.GetLength(0), skipDisplay.GetLength(1), names.GetLength(0), names.GetLength(1));
                        break;
                    }

                    var actualSuspect = solution[0];
                    var actualWeapon = solution[1];
                    var actualRoom = solution[2];
                    var bodyFound = fldBodyFound.Get();
                    if (actualSuspect < 0 || actualSuspect >= 6 || actualWeapon < 0 || actualWeapon >= 6 || actualRoom < 0 || actualRoom >= 9 || bodyFound < 0 || bodyFound >= 9)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Murder: Unexpected suspect, weapon, room or bodyFound (expected 0–5/0–5/0–8/0–8, got {1}/{2}/{3}/{4}).", _SouvenirID, actualSuspect, actualWeapon, actualRoom, bodyFound);
                        break;
                    }

                    addQuestion(Question.MurderSuspect, _Murder,
                        Enumerable.Range(0, 6).Where(suspectIx => skipDisplay[0, suspectIx] == 0 && suspectIx != actualSuspect).Select(suspectIx => names[0, suspectIx]).ToArray(),
                        new[] { "a suspect but not the murderer" });
                    addQuestion(Question.MurderSuspect, _Murder,
                        Enumerable.Range(0, 6).Where(suspectIx => skipDisplay[0, suspectIx] == 1).Select(suspectIx => names[0, suspectIx]).ToArray(),
                        new[] { "not a suspect" });

                    addQuestion(Question.MurderWeapon, _Murder,
                        Enumerable.Range(0, 6).Where(weaponIx => skipDisplay[1, weaponIx] == 0 && weaponIx != actualWeapon).Select(weaponIx => names[1, weaponIx]).ToArray(),
                        new[] { "a potential weapon but not the murder weapon" });
                    addQuestion(Question.MurderWeapon, _Murder,
                        Enumerable.Range(0, 6).Where(weaponIx => skipDisplay[1, weaponIx] == 1).Select(weaponIx => names[1, weaponIx]).ToArray(),
                        new[] { "not a potential weapon" });

                    if (bodyFound != actualRoom)
                        addQuestion(Question.MurderBodyFound, _Murder, new[] { names[2, bodyFound] });

                    break;
                }

            case _Neutralization:
                {
                    var comp = GetComponent(module, "neutralization");
                    var fldAcidType = GetField<int>(comp, "acidType");
                    var fldAcidVol = GetField<int>(comp, "acidVol");
                    var fldSolved = GetField<bool>(comp, "_isSolved");
                    if (comp == null || fldAcidType == null || fldAcidVol == null || fldSolved == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var acidType = fldAcidType.Get();
                    if (acidType < 0 || acidType > 3)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Neutralization: Unexpected acid type: {1}", _SouvenirID, acidType);
                        break;
                    }
                    var acidVol = fldAcidVol.Get();
                    if (acidVol < 5 || acidVol > 20 || acidVol % 5 != 0)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Neutralization: Unexpected acid volume: {1}", _SouvenirID, acidVol);
                        break;
                    }

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_Neutralization);
                    addQuestion(Question.NeutralizationColor, _Neutralization, new[] { new[] { "Yellow", "Green", "Red", "Blue" }[acidType] });
                    addQuestion(Question.NeutralizationVolume, _Neutralization, new[] { acidVol.ToString() });
                    break;
                }

            case _OnlyConnect:
                {
                    var comp = GetComponent(module, "OnlyConnectModule");
                    var fldHieroglyphsDisplayed = GetField<int[]>(comp, "_hieroglyphsDisplayed");
                    var fldIsSolved = GetField<bool>(comp, "_isSolved");
                    if (comp == null || fldHieroglyphsDisplayed == null || fldIsSolved == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var hieroglyphsDisplayed = fldHieroglyphsDisplayed.Get();
                    if (hieroglyphsDisplayed == null || hieroglyphsDisplayed.Length != 6 || hieroglyphsDisplayed.Any(h => h < 0 || h >= 6))
                    {
                        Debug.LogFormat("[Souvenir #{0}] Only Connect: hieroglyphsDisplayed has unexpected value: {1}", _SouvenirID,
                            hieroglyphsDisplayed == null ? "null" : string.Format("[{0}]", hieroglyphsDisplayed.JoinString(", ")));
                        break;
                    }

                    while (!fldIsSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_OnlyConnect);

                    var hieroglyphs = new[] { "Two Reeds", "Lion", "Twisted Flax", "Horned Viper", "Water", "Eye of Horus" };
                    var positions = new[] { "top left", "top middle", "top right", "bottom left", "bottom middle", "bottom right" };
                    for (int i = 0; i < positions.Length; i++)
                        addQuestion(Question.OnlyConnectHieroglyphs, _OnlyConnect, new[] { hieroglyphs[hieroglyphsDisplayed[i]] }, new[] { positions[i] });
                    break;
                }

            case _OrientationCube:
                {
                    var comp = GetComponent(module, "OrientationModule");
                    var fldInitialVirtualViewAngle = GetField<float>(comp, "initialVirtualViewAngle");
                    var fldSubmitButton = GetField<KMSelectable>(comp, "SubmitButton", isPublic: true);
                    var mthGetRule = GetMethod<object>(comp, "GetRule", 0);
                    var mthIsFacing = GetMethod<bool>(comp, "IsFacing", 2);
                    if (comp == null || fldInitialVirtualViewAngle == null || fldSubmitButton == null || mthGetRule == null || mthIsFacing == null)
                        break;

                    // Wait for one frame to ensure Orientation Cube has set initialVirtualViewAngle in its Start()
                    yield return null;

                    var initialVirtualViewAngle = fldInitialVirtualViewAngle.Get();
                    Debug.LogFormat("[Souvenir #{1}] Orientation Cube initialVirtualViewAngle = {0}", initialVirtualViewAngle, _SouvenirID);
                    var initialAnglePos = Array.IndexOf(new[] { 0f, 90f, 180f, 270f }, initialVirtualViewAngle);
                    if (initialAnglePos == -1)
                    {
                        Debug.LogFormat("[Souvenir #{1}] Orientation Cube: initialVirtualViewAngle has unexpected value: {0}", initialVirtualViewAngle, _SouvenirID);
                        break;
                    }

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var submitButton = fldSubmitButton.Get();
                    if (submitButton == null)
                        break;

                    var prevInteract = submitButton.OnInteract;
                    var solved = false;

                    submitButton.OnInteract = delegate
                    {
                        var rule = mthGetRule.Invoke();
                        var fldFromFacing = GetField<Vector3>(rule, "fromFacing", isPublic: true);
                        var fldToFacing = GetField<Vector3>(rule, "toFacing", isPublic: true);
                        var fldHasSecondaryRule = GetField<bool>(rule, "hasSecondaryRule", isPublic: true);
                        var fldSecondaryFromFacing = GetField<Vector3>(rule, "secondaryFromFacing", isPublic: true);
                        var fldSecondaryToFacing = GetField<Vector3>(rule, "secondaryToFacing", isPublic: true);

                        if (rule == null || fldFromFacing == null || fldToFacing == null || fldHasSecondaryRule == null || fldSecondaryFromFacing == null || fldSecondaryToFacing == null || submitButton == null)
                        {
                            Debug.LogFormat("[Souvenir #{0}] Abandoning Orientation Cube.", _SouvenirID);
                            submitButton.OnInteract = prevInteract;
                        }
                        else if (mthIsFacing.Invoke(fldFromFacing.Field.GetValue(rule), fldToFacing.Field.GetValue(rule)) &&
                                !(bool) fldHasSecondaryRule.Field.GetValue(rule) || mthIsFacing.Invoke(fldSecondaryFromFacing.Field.GetValue(rule), fldSecondaryToFacing.Field.GetValue(rule)))
                        {
                            solved = true;
                            submitButton.OnInteract = prevInteract;
                        }
                        return prevInteract();
                    };

                    while (!solved)
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_OrientationCube);

                    addQuestion(Question.OrientationCubeInitialObserverPosition, _OrientationCube, new[] { new[] { "front", "left", "back", "right" }[initialAnglePos] });
                    break;
                }

            case _PerspectivePegs:
                {
                    var comp = GetComponent(module, "PerspectivePegsModule");
                    var fldIsComplete = GetField<bool>(comp, "isComplete");
                    var fldEnteredSequence = GetField<List<int>>(comp, "EnteredSequence");
                    if (comp == null || fldIsComplete == null || fldEnteredSequence == null)
                        break;

                    while (!fldIsComplete.Get())
                        yield return new WaitForSeconds(.1f);
                    _modulesSolved.IncSafe(_PerspectivePegs);

                    var entered = fldEnteredSequence.Get();
                    if (entered == null)
                        break;
                    if (entered.Count != 3 || entered.Any(e => e < 0 || e >= 5))
                    {
                        Debug.LogFormat("[Souvenir #{1}] Perspective Pegs: EnteredSequence has unrecognized member or unexpected length: [{0}]", entered.JoinString(", "), _SouvenirID);
                        break;
                    }

                    var theory = new[] { "top", "top right", "bottom right", "bottom left", "top left" };
                    for (int i = 0; i < 3; i++)
                        addQuestion(Question.PerspectivePegsSolution, _PerspectivePegs, new[] { theory[entered[i]] }, extraFormatArguments: new[] { ordinal(i + 1) }, preferredWrongAnswers: entered.Select(e => theory[e]).ToArray());

                    break;
                }

            case _SeaShells:
                {
                    var comp = GetComponent(module, "SeaShellsModule");
                    var fldRow = GetField<int>(comp, "row");
                    var fldCol = GetField<int>(comp, "col");
                    var fldKeynum = GetField<int>(comp, "keynum");
                    var fldStage = GetField<int>(comp, "stage");
                    var fldSolved = GetField<bool>(comp, "isPassed");
                    var fldDisplay = GetField<TextMesh>(comp, "Display", isPublic: true);

                    if (comp == null || fldRow == null || fldCol == null || fldKeynum == null || fldStage == null || fldSolved == null || fldDisplay == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    var rows = new List<int>();
                    var cols = new List<int>();
                    var keynums = new List<int>();
                    while (true)
                    {
                        while (fldDisplay.Get().text == " ")
                        {
                            yield return new WaitForSeconds(.1f);
                            if (fldSolved.Get())
                                goto solved;
                        }

                        rows.Add(fldRow.Get());
                        cols.Add(fldCol.Get());
                        keynums.Add(fldKeynum.Get());

                        while (fldDisplay.Get().text != " ")
                        {
                            yield return new WaitForSeconds(.1f);
                            if (fldSolved.Get())
                                goto solved;
                        }
                    }

                    solved:
                    _modulesSolved.IncSafe(_SeaShells);

                    for (int i = 0; i < rows.Count; i++)
                    {
                        addQuestion(Question.SeaShells1, _SeaShells, new[] { new[] { "she sells", "she shells", "sea shells", "sea sells" }[rows[i]] }, new[] { ordinal(i + 1) });
                        addQuestion(Question.SeaShells2, _SeaShells, new[] { new[] { "sea shells", "she shells", "sea sells", "she sells" }[cols[i]] }, new[] { ordinal(i + 1) });
                        addQuestion(Question.SeaShells3, _SeaShells, new[] { new[] { "sea shore", "she sore", "she sure", "seesaw" }[keynums[i]] }, new[] { ordinal(i + 1) });
                    }
                    break;
                }

            case _SillySlots:
                {
                    var comp = GetComponent(module, "SillySlots");
                    var fldSolved = GetField<bool>(comp, "solved");
                    var fldPrevSlots = GetField<IList>(comp, "mPreviousSlots");

                    if (comp == null || fldSolved == null || fldPrevSlots == null)
                        break;

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_SillySlots);

                    var prevSlots = fldPrevSlots.Get();
                    if (prevSlots == null)
                        break;
                    if (prevSlots.Count < 2)
                    {
                        // Legitimate: first stage was a keep already
                        Debug.LogFormat("[Souvenir #{0}] No question for Silly Slots because there was only one stage.", _SouvenirID);
                        break;
                    }

                    if (prevSlots.Cast<object>().Any(obj => !(obj is Array) || ((Array) obj).Length != 3))
                    {
                        Debug.LogFormat("[Souvenir #{0}] Abandoning Silly Slots because prevSlots {1}.",
                            _SouvenirID,
                            prevSlots == null ? "is null" :
                            prevSlots.Count == 0 ? "has length 0" :
                            string.Format("has an unexpected item (expected arrays of length 3): [{0}]", prevSlots.Cast<object>().Select(obj => obj == null ? "<null>" : !(obj is Array) ? string.Format("<{0}>", obj.GetType().FullName) : string.Format("<Array, length={0}>", ((Array) obj).Length)).JoinString(", ")));
                        break;
                    }

                    var testSlot = ((Array) prevSlots[0]).GetValue(0);
                    var fldShape = GetField<object>(testSlot, "shape", isPublic: true);
                    var fldColor = GetField<object>(testSlot, "color", isPublic: true);
                    if (fldShape == null || fldColor == null)
                        break;

                    // Skip the last stage because if the last action was Keep, it is still visible on the module
                    for (int stage = 0; stage < prevSlots.Count - 1; stage++)
                    {
                        var slotStrings = ((Array) prevSlots[stage]).Cast<object>().Select(obj => (fldColor.Field.GetValue(obj).ToString() + " " + fldShape.Field.GetValue(obj).ToString()).ToLowerInvariant()).ToArray();
                        for (int slot = 0; slot < slotStrings.Length; slot++)
                            addQuestion(Question.SillySlots, _SillySlots, new[] { slotStrings[slot] }, new[] { ordinal(slot + 1), ordinal(stage + 1) }, slotStrings);
                    }

                    break;
                }

            case _SimonScreams:
                {
                    var comp = GetComponent(module, "SimonScreamsModule");
                    var fldSequences = GetField<int[][]>(comp, "_sequences");
                    var fldColors = GetField<Array>(comp, "_colors");
                    var fldSolved = GetField<bool>(comp, "_isSolved");

                    if (comp == null || fldSequences == null || fldColors == null || fldSolved == null)
                        break;

                    while (!fldSolved.Get())
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_SimonScreams);

                    var seqs = fldSequences.Get();
                    var colors = fldColors.Get();
                    if (seqs == null || colors == null)
                        break;
                    if (seqs.Length == 0)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Abandoning Simon Screams because _sequences has a zero length.", _SouvenirID);
                        break;
                    }
                    if (colors.Length != 6)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Abandoning Simon Screams because _colors has length {1} (expected 6).", _SouvenirID, colors.Length);
                        break;
                    }

                    var lastSeq = seqs.Last();
                    for (int i = 0; i < lastSeq.Length; i++)
                        addQuestion(Question.SimonScreamsFlashing, _SimonScreams, new[] { colors.GetValue(lastSeq[i]).ToString() }, new[] { ordinal(i + 1) });

                    // First determine which rule applied in which stage
                    var ryb = new[] { "Red", "Yellow", "Blue" }.Select(colorName => colors.IndexOf(c => c.ToString() == colorName)).ToArray();
                    var stageRules = new int[seqs.Length];
                    for (int i = 0; i < seqs.Length; i++)
                    {
                        var seq = seqs[i];
                        // "If three adjacent colors flashed in clockwise order"
                        if (Enumerable.Range(0, seq.Length - 2).Any(ix => seq[ix + 1] == (seq[ix] + 1) % 6 && seq[ix + 2] == (seq[ix] + 2) % 6))
                            stageRules[i] = 0;
                        // "Otherwise, if a color flashed, then an adjacent color, then the first again"
                        else if (Enumerable.Range(0, seq.Length - 2).Any(ix => seq[ix + 2] == seq[ix] && (seq[ix + 1] == (seq[ix] + 1) % 6 || seq[ix + 1] == (seq[ix] + 5) % 6)))
                            stageRules[i] = 1;
                        // "Otherwise, if at most one color flashed out of red, yellow, and blue"
                        else if (ryb.Count(colIx => seq.Contains(colIx)) <= 1)
                            stageRules[i] = 2;
                        // "Otherwise, if there are two colors opposite each other that didn’t flash"
                        else if (Enumerable.Range(0, 3).Any(col => !seq.Contains(col) && !seq.Contains(col + 3)))
                            stageRules[i] = 3;
                        // "Otherwise, if two adjacent colors flashed in clockwise order"
                        else if (Enumerable.Range(0, seq.Length - 1).Any(ix => seq[ix + 1] == (seq[ix] + 1) % 6))
                            stageRules[i] = 4;
                        // "Otherwise"
                        else
                            stageRules[i] = 5;
                    }

                    // Note that we’re excluding the Otherwise row
                    var ruleNames = Ut.NewArray(
                        "three adjacent colors flashing in clockwise order",
                        "a color flashing, then an adjacent color, then the first again",
                        "at most one color flashing out of red, yellow, and blue",
                        "two colors opposite each other that didn’t flash",
                        "two (but not three) adjacent colors flashing in clockwise order"
                    );
                    // Now set the questions
                    for (int rule = 0; rule < ruleNames.Length; rule++)
                    {
                        var applicableStages = new List<string>();
                        for (int stage = 0; stage < stageRules.Length; stage++)
                            if (stageRules[stage] == rule)
                                applicableStages.Add(ordinal(stage + 1));
                        if (applicableStages.Count > 0)
                            addQuestion(Question.SimonScreamsRule, _SimonScreams,
                                new[] { applicableStages.Count == stageRules.Length ? "all of them" : applicableStages.JoinString(", ", lastSeparator: " and ") },
                                new[] { applicableStages.Count == 1 ? "stage" : "stages", ruleNames[rule] },
                                applicableStages.Count == 1
                                    ? Enumerable.Range(1, seqs.Length).Select(i => ordinal(i)).ToArray()
                                    : Enumerable.Range(1, seqs.Length).SelectMany(a => Enumerable.Range(a + 1, seqs.Length - a).Select(b => ordinal(a) + " and " + ordinal(b))).Concat(new[] { "all of them" }).ToArray());
                    }

                    break;
                }

            case _SimonStates:
                {
                    var comp = GetComponent(module, "AdvancedSimon");
                    var fldPuzzleDisplay = GetField<bool[][]>(comp, "PuzzleDisplay");
                    var fldAnswer = GetField<int[]>(comp, "Answer");
                    var fldProgress = GetField<int>(comp, "Progress");

                    if (comp == null || fldPuzzleDisplay == null || fldAnswer == null || fldProgress == null)
                        break;

                    bool[][] puzzleDisplay;
                    while ((puzzleDisplay = fldPuzzleDisplay.Get(nullAllowed: true)) == null)
                        yield return new WaitForSeconds(.1f);

                    if (puzzleDisplay.Length != 4 || puzzleDisplay.Any(arr => arr.Length != 4))
                    {
                        Debug.LogFormat("[Souvenir #{1}] Abandoning Simon States because PuzzleDisplay has an unexpected length or value: [{0}]",
                            puzzleDisplay.Select(arr => arr == null ? "null" : "[" + arr.JoinString(", ") + "]").JoinString("; "), _SouvenirID);
                        break;
                    }

                    var colorNames = new[] { "Red", "Yellow", "Green", "Blue" };
                    Debug.LogFormat("[Souvenir #{1}] Simon States: PuzzleDisplay = [{0}]",
                        puzzleDisplay.Select(arr => arr.Select((v, i) => v ? colorNames[i] : null).Where(x => x != null).JoinString(", ")).JoinString("; ", "[", "]"), _SouvenirID);

                    while (fldProgress.Get() < 4)
                        yield return new WaitForSeconds(.1f);
                    // Consistency check
                    if (fldPuzzleDisplay.Get(nullAllowed: true) != null)
                    {
                        Debug.LogFormat("[Souvenir #{0}] Abandoning Simon States because PuzzleDisplay was expected to be null when Progress reached 4, but wasn’t.", _SouvenirID);
                        break;
                    }

                    _modulesSolved.IncSafe(_SimonStates);

                    for (int i = 0; i < 4; i++)
                    {
                        var c = puzzleDisplay[i].Count(b => b);
                        if (c != 3)
                            addQuestion(Question.SimonStatesDisplay, _SimonStates,
                                new[] { c == 4 ? "all 4" : puzzleDisplay[i].Select((v, j) => v ? colorNames[j] : null).Where(x => x != null).JoinString(", ") },
                                new[] { "color(s) flashed", ordinal(i + 1) });
                        if (c != 1)
                            addQuestion(Question.SimonStatesDisplay, _SimonStates,
                                new[] { c == 4 ? "none" : puzzleDisplay[i].Select((v, j) => v ? null : colorNames[j]).Where(x => x != null).JoinString(", ") },
                                new[] { "color(s) didn’t flash", ordinal(i + 1) });
                    }

                    break;
                }

            case _SkewedSlots:
                {
                    var comp = GetComponent(module, "SkewedModule");
                    var fldNumbers = GetField<int[]>(comp, "Numbers");
                    var fldModuleActivated = GetField<bool>(comp, "moduleActivated");
                    var fldSolved = GetField<bool>(comp, "solved");

                    if (comp == null || fldNumbers == null || fldModuleActivated == null || fldSolved == null)
                        break;

                    var originalNumbers = new List<string>();

                    while (true)
                    {
                        // Skewed Slots sets moduleActivated to false while the slots are spinning.
                        // If there was a correct answer, it will set solved to true, otherwise it will set moduleActivated to true.
                        while (!fldModuleActivated.Get() && !fldSolved.Get())
                            yield return new WaitForSeconds(.1f);

                        if (fldSolved.Get())
                            break;

                        // Get the current original digits.
                        var numbers = fldNumbers.Get();
                        if (numbers == null)
                            break;
                        if (numbers.Length != 3 || numbers.Any(n => n < 0 || n > 9))
                        {
                            Debug.LogFormat("[Souvenir #{0}] Abandoning Skewed Slots because numbers has unexpected length (3) or a number outside expected range (0–9): [{1}].", _SouvenirID, numbers.JoinString(", "));
                            goto abandonSkewedSlots;
                        }
                        originalNumbers.Add(numbers.JoinString());

                        // When the user presses anything, Skewed Slots sets moduleActivated to false while the slots are spinning.
                        while (fldModuleActivated.Get())
                            yield return new WaitForSeconds(.1f);
                    }

                    _modulesSolved.IncSafe(_SkewedSlots);

                    for (int i = 0; i < originalNumbers.Count; i++)
                        addQuestion(Question.SkewedSlotsOriginalNumbers, _SkewedSlots, new[] { originalNumbers[i] },
                            extraFormatArguments: new[] { originalNumbers.Count == 1 ? "" : ordinal(i + 1) + " " },
                            preferredWrongAnswers: originalNumbers.Concat(Enumerable.Range(0, int.MaxValue).Select(_ => Rnd.Range(0, 1000).ToString("000"))).Where(str => str != originalNumbers[i]).Distinct().Take(5).ToArray());

                    abandonSkewedSlots:
                    break;
                }

            case _TheBulb:
                {
                    var comp = GetComponent(module, "TheBulbModule");
                    var fldButtonPresses = GetField<string>(comp, "_correctButtonPresses");
                    var fldStage = GetField<int>(comp, "_stage");

                    if (comp == null || fldButtonPresses == null || fldStage == null)
                        break;

                    while (!_isActivated)
                        yield return new WaitForSeconds(.1f);

                    while (fldStage.Get() != 0)
                        yield return new WaitForSeconds(.1f);
                    _modulesSolved.IncSafe(_TheBulb);

                    var buttonPresses = fldButtonPresses.Get();
                    if (buttonPresses == null || buttonPresses.Length != 3)
                    {
                        Debug.LogFormat("[Souvenir #{0}] The Bulb: _correctButtonPresses has unexpected value ({1})", _SouvenirID, buttonPresses == null ? "<null>" : string.Format(@"""{0}""", buttonPresses));
                        break;
                    }

                    addQuestion(Question.TheBulbButtonPresses, _TheBulb, new[] { buttonPresses });
                    break;
                }

            case _TwoBits:
                {
                    var comp = GetComponent(module, "TwoBitsModule");
                    var fldFirstQueryCode = GetField<int>(comp, "firstQueryCode");
                    var fldQueryLookups = GetField<Dictionary<int, string>>(comp, "queryLookups");
                    var fldQueryResponses = GetField<Dictionary<string, int>>(comp, "queryResponses");
                    var fldCurrentState = GetField<object>(comp, "currentState");

                    if (comp == null || fldFirstQueryCode == null || fldQueryLookups == null || fldQueryResponses == null || fldCurrentState == null)
                        break;

                    while (fldCurrentState.Get().ToString() != "Complete")
                        yield return new WaitForSeconds(.1f);

                    _modulesSolved.IncSafe(_TwoBits);

                    var queryLookups = fldQueryLookups.Get();
                    var queryResponses = fldQueryResponses.Get();
                    if (queryLookups == null || queryResponses == null)
                        break;

                    try
                    {
                        var zerothNumCode = fldFirstQueryCode.Get();
                        var zerothLetterCode = queryLookups[zerothNumCode];
                        var firstResponse = queryResponses[zerothLetterCode];
                        var firstLookup = queryLookups[firstResponse];
                        var secondResponse = queryResponses[firstLookup];
                        var secondLookup = queryLookups[secondResponse];
                        var thirdResponse = queryResponses[secondLookup];
                        var preferredWrongAnswers = new[] { zerothNumCode.ToString("00"), firstResponse.ToString("00"), secondResponse.ToString("00"), thirdResponse.ToString("00") };
                        addQuestion(Question.TwoBitsResponse, _TwoBits, new[] { firstResponse.ToString("00") }, new[] { "first" }, preferredWrongAnswers);
                        addQuestion(Question.TwoBitsResponse, _TwoBits, new[] { secondResponse.ToString("00") }, new[] { "second" }, preferredWrongAnswers);
                        addQuestion(Question.TwoBitsResponse, _TwoBits, new[] { thirdResponse.ToString("00") }, new[] { "third" }, preferredWrongAnswers);
                    }
                    catch (Exception e)
                    {
                        Debug.LogFormat("[Souvenir #{2}] Two bits: Exception: {0} ({1})", e.Message, e.GetType().FullName, _SouvenirID);
                    }

                    break;
                }

            case _Souvenir:
                {
                    _waitableModules--;
                    break;
                }

            default:
                if (_isTimwisComputer && !_ignoreModules.Contains(module.name))
                {
                    var s = new StringBuilder();
                    s.AppendLine("Unrecognized module: " + module.name);
                    foreach (var comp in module.GetComponents(typeof(UnityEngine.Object)))
                        s.AppendLine("    - " + comp.GetType().FullName);
                    lock (_timwiPath)
                        File.AppendAllText(_timwiPath, s.ToString());
                }
                break;
        }

        Debug.LogFormat("[Souvenir #{1}] Finished processing {0}.", module.name, _SouvenirID);
    }

    private string titleCase(string str)
    {
        return str.Length < 1 ? str : char.ToUpperInvariant(str[0]) + str.Substring(1).ToLowerInvariant();
    }

    private Dictionary<Question, SouvenirQuestionAttribute> _attributes;

    private void addQuestion(Question question, string moduleKey, string[] possibleCorrectAnswers, string[] extraFormatArguments = null, string[] preferredWrongAnswers = null, int? unleashAt = null)
    {
        _questions.AddSafe(moduleKey, questionMaker(question, moduleKey, possibleCorrectAnswers, extraFormatArguments, preferredWrongAnswers, unleashAt)(_modulesSolved.Get(moduleKey)));
    }

    private Func<int, QandA> questionMaker(Question question, string moduleKey, string[] possibleCorrectAnswers, string[] extraFormatArguments = null, string[] preferredWrongAnswers = null, int? unleashAt = null)
    {
        SouvenirQuestionAttribute attr;
        if (!_attributes.TryGetValue(question, out attr))
        {
            Debug.LogFormat("[Souvenir #{1}] Question {0} has no attribute.", question, _SouvenirID);
            return null;
        }

        var allAnswers = attr.AllAnswers;
        if (allAnswers != null)
        {
            var inconsistency = possibleCorrectAnswers.FirstOrDefault(pca => !allAnswers.Contains(pca));
            if (inconsistency != null)
            {
                Debug.LogFormat("[Souvenir #{2}] Question {0}: invalid answer: {1}.", question, inconsistency, _SouvenirID);
                return null;
            }
            if (preferredWrongAnswers != null)
            {
                var inconsistency2 = preferredWrongAnswers.FirstOrDefault(pca => !allAnswers.Contains(pca));
                if (inconsistency2 != null)
                {
                    Debug.LogFormat("[Souvenir #{2}] Question {0}: invalid preferred wrong answer: {1}.", question, inconsistency2, _SouvenirID);
                    return null;
                }
            }
        }

        return solvedOrd =>
        {
            var num = _moduleCounts.Get(moduleKey);

            List<string> answers;
            if (allAnswers == null)
                answers = preferredWrongAnswers.Except(possibleCorrectAnswers).ToList().Shuffle().Take(attr.NumAnswers - 1).ToList();
            else
            {
                // Pick 𝑛−1 random wrong answers.
                answers = allAnswers.Except(possibleCorrectAnswers).ToList().Shuffle().Take(attr.NumAnswers - 1).ToList();
                // Add the preferred wrong answers, if any. If we had added them earlier, they’d come up too rarely.
                if (preferredWrongAnswers != null)
                    answers = answers.Concat(preferredWrongAnswers.Except(possibleCorrectAnswers)).ToList().Shuffle().Take(attr.NumAnswers - 1).ToList();
            }

            var correctIndex = Rnd.Range(0, Math.Min(attr.NumAnswers, answers.Count + 1));
            answers.Insert(correctIndex, possibleCorrectAnswers[Rnd.Range(0, possibleCorrectAnswers.Length)]);

            var formatArguments = new List<string> { num > 1 ? string.Format("the {0} you solved {1}", attr.ModuleName, ordinal(solvedOrd)) : attr.AddThe ? "The\u00a0" + attr.ModuleName : attr.ModuleName };
            if (extraFormatArguments != null)
                formatArguments.AddRange(extraFormatArguments);

            var q = new QandA(
                string.Format(attr.QuestionText, formatArguments.ToArray()),
                answers.ToArray(),
                correctIndex,
                unleashAt ?? (Bomb.GetSolvedModuleNames().Count + 1));
            Debug.LogFormat("[Souvenir #{1}] Making question {2}: {0}", q.DebugString, _SouvenirID, question);
            return q;
        };
    }

    private string ordinal(int number)
    {
        if (number < 0)
            return "(" + number + ")th";

        switch (number)
        {
            case 1: return "first";
            case 2: return "second";
            case 3: return "third";
        }

        switch ((number / 10) % 10 == 1 ? 0 : number % 10)
        {
            case 1: return number + "st";
            case 2: return number + "nd";
            case 3: return number + "rd";
            default: return number + "th";
        }
    }

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        if ((command.Equals("thanks", StringComparison.InvariantCultureIgnoreCase) || command.Equals("thank you", StringComparison.InvariantCultureIgnoreCase)) &&
            Answers4Parent.activeSelf && _currentQuestion.QuestionText == "Congratulations!")
            return new[] { Answers4[0] };

        var m = Regex.Match(command, @"\A\s*answer\s+(\d)\s*\z");
        if (!m.Success)
            return null;

        var number = int.Parse(m.Groups[1].Value);
        var btns = Answers4Parent.activeSelf ? Answers4 : Answers6;
        if (number <= 0 || number > btns.Length)
            return null;
        var btn = btns[number - 1];
        if (btn == null || !btn.gameObject.activeSelf)
            return null;
        return new[] { btn };
    }
}
