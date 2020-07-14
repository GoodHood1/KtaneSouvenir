using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using KModkit;
using Newtonsoft.Json;
using Souvenir;
using Souvenir.Reflection;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Souvenir
/// Created by Timwi
/// </summary>
public class SouvenirModule : MonoBehaviour
{
    #region Fields
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMBossModule BossModule;
    public KMModSettings ModSettings;
    public KMSelectable[] Answers;
    public GameObject AnswersParent;
    public GameObject[] TpNumbers;
    public Sprite[] KeypadSprites;
    public Sprite[] MemorySprites;
    public Sprite[] ArithmelogicSprites;
    public Sprite[] ExampleSprites;
    public Sprite[] MahjongSprites;
    public Sprite[] PatternCubeSprites;
    public Sprite[] PerspectivePegsSprites;
    public Sprite[] PlanetsSprites;
    public Sprite[] SymbolicCoordinatesSprites;
    public Sprite[] WavetappingSprites;
    public Sprite[] FlagsSprites;
    public Sprite[] Tiles4x4Sprites;    // Arranged in reading order
    public Sprite[] EncryptedEquationsSprites;

    public TextMesh TextMesh;
    public Renderer TextRenderer;
    public Renderer SurfaceRenderer;
    public GameObject WarningIcon;
    public Material FontMaterial;
    public Font[] Fonts;
    public Texture[] FontTextures;
    public Mesh HighlightShort; // 6 answers, 2 columns
    public Mesh HighlightLong;  // 4 answers, 2 columns
    public Mesh HighlightVeryLong;  // 4 long answers, 1 column

    /// <summary>May be set to a question name while playing the test harness to skip to that question.</summary>
    public string TestQuestion;
    /// <summary>May be used if the prefab of a different module is available in the project</summary>
    public bool ModulePresent;

    public static readonly string[] _defaultIgnoredModules = {
        "Souvenir",
        "Forget Everything",
        "Forget Me Not",
        "Forget This",
        "Turn The Key",
        "The Time Keeper",
        "Simon's Stages",
        "Purgatory"
    };

    private Config config;
    private readonly List<QuestionBatch> _questions = new List<QuestionBatch>();
    private readonly HashSet<KMBombModule> _legitimatelyNoQuestions = new HashSet<KMBombModule>();
    private readonly HashSet<string> supportedModuleNames = new HashSet<string>();
    private readonly HashSet<string> ignoredModules = new HashSet<string>();
    private bool _isActivated = false;

    private QandA _currentQuestion = null;
    private bool _isSolved = false;
    private bool _animating = false;
    private bool _exploded = false;
    private int _avoidQuestions = 0;   // While this is > 0, temporarily avoid asking questions; currently only used when Souvenir is hidden by a Mystery Module

    [NonSerialized]
    public double SurfaceSizeFactor;

    private Dictionary<string, int> _moduleCounts = new Dictionary<string, int>();
    private Dictionary<string, int> _modulesSolved = new Dictionary<string, int>();
    private int _coroutinesActive;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Dictionary<string, Func<KMBombModule, IEnumerable<object>>> _moduleProcessors;
    private Dictionary<Question, SouvenirQuestionAttribute> _attributes;
    #endregion

    #region Module ID constant declarations
    // The values here are the “ModuleType” property on the KMBombModule components.
    const string _3DMaze = "spwiz3DMaze";
    const string _3DTunnels = "3dTunnels";
    const string _Accumulation = "accumulation";
    const string _AdventureGame = "spwizAdventureGame";
    const string _AffineCycle = "affineCycle";
    const string _Algebra = "algebra";
    const string _AlphabeticalRuling = "alphabeticalRuling";
    const string _AlphaBits = "alphaBits";
    const string _Arithmelogic = "arithmelogic";
    const string _BamboozledAgain = "bamboozledAgain";
    const string _BamboozlingButton = "bamboozlingButton";
    const string _Bartending = "BartendingModule";
    const string _BigCircle = "BigCircle";
    const string _BinaryLEDs = "BinaryLeds";
    const string _Bitmaps = "BitmapsModule";
    const string _BlindMaze = "BlindMaze";
    const string _Blockbusters = "blockbusters";
    const string _BlueArrows = "blueArrowsModule";
    const string _BobBarks = "ksmBobBarks";
    const string _Boggle = "boggle";
    const string _Boxing = "boxing";
    const string _Braille = "BrailleModule";
    const string _BrokenButtons = "BrokenButtonsModule";
    const string _BrushStrokes = "brushStrokes";
    const string _Bulb = "TheBulbModule";
    const string _BurglarAlarm = "burglarAlarm";
    const string _Button = "BigButton";
    const string _ButtonSequences = "buttonSequencesModule";
    const string _CaesarCycle = "caesarCycle";
    const string _Calendar = "calendar";
    const string _ChallengeAndContact = "challengeAndContact";
    const string _CheapCheckout = "CheapCheckoutModule";
    const string _Chess = "ChessModule";
    const string _ChineseCounting = "chineseCounting";
    const string _ChordQualities = "ChordQualities";
    const string _Code = "theCodeModule";
    const string _Coffeebucks = "coffeebucks";
    const string _ColorBraille = "ColorBrailleModule";
    const string _ColorDecoding = "Color Decoding";
    const string _ColoredKeys = "lgndColoredKeys";
    const string _ColoredSquares = "ColoredSquaresModule";
    const string _ColoredSwitches = "ColoredSwitchesModule";
    const string _ColorMorse = "ColorMorseModule";
    const string _Coordinates = "CoordinatesModule";
    const string _Corners = "CornersModule";
    const string _Crackbox = "CrackboxModule";
    const string _Creation = "CreationModule";
    const string _CrypticCycle = "crypticCycle";
    const string _Cube = "cube";
    const string _DACHMaze = "DACH";
    const string _DeckOfManyThings = "deckOfManyThings";
    const string _DecoloredSquares = "DecoloredSquaresModule";
    const string _DiscoloredSquares = "DiscoloredSquaresModule";
    const string _DoubleColor = "doubleColor";
    const string _DoubleOh = "DoubleOhModule";
    const string _DrDoctor = "DrDoctorModule";
    const string _ElderFuthark = "elderFuthark";
    const string _EncryptedEquations = "EncryptedEquationsModule";
    const string _EncryptedHangman = "encryptedHangman";
    const string _EncryptedMorse = "EncryptedMorse";
    const string _EquationsX = "equationsXModule";
    const string _Etterna = "etterna";
    const string _FactoryMaze = "factoryMaze";
    const string _FastMath = "fastMath";
    const string _FaultyRGBMaze = "faultyrgbMaze";
    const string _Flags = "FlagsModule";
    const string _FlashingLights = "flashingLights";
    const string _ForgetTheColors = "ForgetTheColors";
    const string _FreeParking = "freeParking";
    const string _Functions = "qFunctions";
    const string _Gamepad = "TheGamepadModule";
    const string _GiantsDrink = "giantsDrink";
    const string _GreenArrows = "greenArrowsModule";
    const string _GridLock = "GridlockModule";
    const string _Gryphons = "gryphons";
    const string _LogicalButtons = "logicalButtonsModule";
    const string _HereditaryBaseNotation = "hereditaryBaseNotationModule";
    const string _Hexabutton = "hexabutton";
    const string _Hexamaze = "HexamazeModule";
    const string _HexOS = "hexOS";
    const string _HiddenColors = "lgndHiddenColors";
    const string _HillCycle = "hillCycle";
    const string _Hogwarts = "HogwartsModule";
    const string _HorribleMemory = "horribleMemory";
    const string _HumanResources = "HumanResourcesModule";
    const string _Hunting = "hunting";
    const string _Hypercube = "TheHypercubeModule";
    const string _IceCream = "iceCreamModule";
    const string _IdentityParade = "identityParade";
    const string _Instructions = "instructions";
    const string _iPhone = "iPhone";
    const string _JewelVault = "jewelVault";
    const string _JumbleCycle = "jumbleCycle";
    const string _Kudosudoku = "KudosudokuModule";
    const string _Lasers = "lasers";
    const string _LEDEncryption = "LEDEnc";
    const string _LEDMath = "lgndLEDMath";
    const string _LEGOs = "LEGOModule";
    const string _Listening = "Listening";
    const string _LogicGates = "logicGates";
    const string _LondonUnderground = "londonUnderground";
    const string _Mafia = "MafiaModule";
    const string _Mahjong = "MahjongModule";
    const string _MaritimeFlags = "MaritimeFlagsModule";
    const string _Maze = "Maze";
    const string _Maze3 = "maze3";
    const string _Mazematics = "mazematics";
    const string _MazeScrambler = "MazeScrambler";
    const string _MegaMan2 = "megaMan2";
    const string _MelodySequencer = "melodySequencer";
    const string _MemorableButtons = "memorableButtons";
    const string _Memory = "Memory";
    const string _Microcontroller = "Microcontroller";
    const string _Minesweeper = "MinesweeperModule";
    const string _ModernCipher = "modernCipher";
    const string _ModuleMaze = "ModuleMaze";
    const string _MonsplodeFight = "monsplodeFight";
    const string _MonsplodeTradingCards = "monsplodeCards";
    const string _Moon = "moon";
    const string _MorseAMaze = "MorseAMaze";
    const string _MorseButtons = "morseButtons";
    const string _Morsematics = "MorseV2";
    const string _MorseWar = "MorseWar";
    const string _MouseInTheMaze = "MouseInTheMaze";
    const string _Murder = "murder";
    const string _MysticSquare = "MysticSquareModule";
    const string _MysteryModule = "mysterymodule";
    const string _Necronomicon = "necronomicon";
    const string _Neutralization = "neutralization";
    const string _NandMs = "NandMs";
    const string _Navinums = "navinums";
    const string _NotButton = "NotButton";
    const string _NotKeypad = "NotKeypad";
    const string _NotMaze = "NotMaze";
    const string _NotMorseCode = "NotMorseCode";
    const string _NotSimaze = "NotSimaze";
    const string _NotWhosOnFirst = "NotWhosOnFirst";
    const string _NumberedButtons = "numberedButtonsModule";
    const string _ObjectShows = "objectShows";
    const string _OddOneOut = "OddOneOutModule";
    const string _OnlyConnect = "OnlyConnectModule";
    const string _OrangeArrows = "orangeArrowsModule";
    const string _OrderedKeys = "orderedKeys";
    const string _OrientationCube = "OrientationCube";
    const string _Palindromes = "palindromes";
    const string _PartialDerivatives = "partialDerivatives";
    const string _PassportControl = "passportControl";
    const string _PatternCube = "PatternCubeModule";
    const string _PerspectivePegs = "spwizPerspectivePegs";
    const string _Pie = "pieModule";
    const string _PigpenCycle = "pigpenCycle";
    const string _PlaceholderTalk = "placeholderTalk";
    const string _Planets = "planets";
    const string _PlayfairCycle = "playfairCycle";
    const string _Poetry = "poetry";
    const string _PolyhedralMaze = "PolyhedralMazeModule";
    const string _Probing = "Probing";
    const string _PurpleArrows = "purpleArrowsModule";
    const string _Quintuples = "quintuples";
    const string _RecoloredSwitches = "R4YRecoloredSwitches";
    const string _RedArrows = "redArrowsModule";
    const string _Retirement = "retirement";
    const string _ReverseMorse = "reverseMorse";
    const string _RGBMaze = "rgbMaze";
    const string _Rhythms = "MusicRhythms";
    const string _RoleReversal = "roleReversal";
    const string _Rule = "theRule";
    const string _ScavengerHunt = "scavengerHunt";
    const string _SchlagDenBomb = "qSchlagDenBomb";
    const string _SeaShells = "SeaShells";
    const string _Semamorse = "semamorse";
    const string _ShapesBombs = "ShapesBombs";
    const string _ShapeShift = "shapeshift";
    const string _ShellGame = "shellGame";
    const string _SillySlots = "SillySlots";
    const string _SimonSamples = "simonSamples";
    const string _SimonSays = "Simon";
    const string _SimonScrambles = "simonScrambles";
    const string _SimonScreams = "SimonScreamsModule";
    const string _SimonSelects = "simonSelectsModule";
    const string _SimonSends = "SimonSendsModule";
    const string _SimonShrieks = "SimonShrieksModule";
    const string _SimonSimons = "simonSimons";
    const string _SimonSings = "SimonSingsModule";
    const string _SimonSounds = "simonSounds";
    const string _SimonSpeaks = "SimonSpeaksModule";
    const string _SimonsStar = "simonsStar";
    const string _SimonStates = "SimonV2";
    const string _SimonStops = "simonStops";
    const string _SimonStores = "simonStores";
    const string _SkewedSlots = "SkewedSlotsModule";
    const string _Skyrim = "skyrim";
    const string _Snooker = "snooker";
    const string _SonicTheHedgehog = "sonic";
    const string _Sorting = "sorting";
    const string _Souvenir = "SouvenirModule";
    const string _Sphere = "sphere";
    const string _SplittingTheLoot = "SplittingTheLootModule";
    const string _Switch = "BigSwitch";
    const string _Switches = "switchModule";
    const string _SymbolCycle = "SymbolCycleModule";
    const string _SymbolicCoordinates = "symbolicCoordinates";
    const string _Synonyms = "synonyms";
    const string _TapCode = "tapCode";
    const string _TashaSqueals = "tashaSqueals";
    const string _TenButtonColorCode = "TenButtonColorCode";
    const string _TextField = "TextField";
    const string _ThinkingWires = "thinkingWiresModule";
    const string _ThirdBase = "ThirdBase";
    const string _TicTacToe = "TicTacToeModule";
    const string _Timezone = "timezone";
    const string _TransmittedMorse = "transmittedMorseModule";
    const string _TurtleRobot = "turtleRobot";
    const string _TwoBits = "TwoBits";
    const string _UltimateCycle = "ultimateCycle";
    const string _Ultracube = "TheUltracubeModule";
    const string _UncoloredSquares = "UncoloredSquaresModule";
    const string _UncoloredSwitches = "R4YUncoloredSwitches";
    const string _UnfairCipher = "unfairCipher";
    const string _UnownCipher = "UnownCipher";
    const string _USAMaze = "USA";
    const string _VaricoloredSquares = "VaricoloredSquaresModule";
    const string _Vcrcs = "VCRCS";
    const string _Vectors = "vectorsModule";
    const string _Vexillology = "vexillology";
    const string _VisualImpairment = "visual_impairment";
    const string _Wavetapping = "Wavetapping";
    const string _WhosOnFirst = "WhosOnFirst";
    const string _Wire = "wire";
    const string _WireOrdering = "kataWireOrdering";
    const string _WireSequence = "WireSequence";
    const string _Yahtzee = "YahtzeeModule";
    const string _YellowArrows = "yellowArrowsModule";
    const string _Zoni = "lgndZoni";
    #endregion

    #region Souvenir’s own module logic
    void Start()
    {
        _moduleId = _moduleIdCounter;
        _moduleIdCounter++;

        _moduleProcessors = new Dictionary<string, Func<KMBombModule, IEnumerable<object>>>()
        {
            { _3DMaze, Process3DMaze },
            { _3DTunnels, Process3DTunnels },
            { _Accumulation, ProcessAccumulation },
            { _AdventureGame, ProcessAdventureGame },
            { _AffineCycle, ProcessAffineCycle },
            { _Algebra, ProcessAlgebra },
            { _AlphabeticalRuling, ProcessAlphabeticalRuling },
            { _AlphaBits, ProcessAlphaBits },
            { _Arithmelogic, ProcessArithmelogic },
            { _BamboozledAgain, ProcessBamboozledAgain },
            { _BamboozlingButton, ProcessBamboozlingButton },
            { _Bartending, ProcessBartending },
            { _BigCircle, ProcessBigCircle },
            { _BinaryLEDs, ProcessBinaryLEDs },
            { _Bitmaps, ProcessBitmaps },
            { _BlindMaze, ProcessBlindMaze },
            { _Blockbusters, ProcessBlockbusters },
            { _BlueArrows, ProcessBlueArrows },
            { _BobBarks, ProcessBobBarks },
            { _Boggle, ProcessBoggle },
            { _Boxing, ProcessBoxing },
            { _Braille, ProcessBraille },
            { _BrokenButtons, ProcessBrokenButtons },
            { _BrushStrokes, ProcessBrushStrokes },
            { _Bulb, ProcessBulb },
            { _BurglarAlarm, ProcessBurglarAlarm },
            { _Button, ProcessButton },
            { _ButtonSequences, ProcessButtonSequences },
            { _CaesarCycle, ProcessCaesarCycle },
            { _Calendar, ProcessCalendar },
            { _ChallengeAndContact, ProcessChallengeAndContact },
            { _CheapCheckout, ProcessCheapCheckout },
            { _Chess, ProcessChess },
            { _ChineseCounting, ProcessChineseCounting },
            { _ChordQualities, ProcessChordQualities },
            { _Code, ProcessCode },
            { _Coffeebucks, ProcessCoffeebucks },
            { _ColorBraille, ProcessColorBraille },
            { _ColorDecoding, ProcessColorDecoding },
            { _ColoredKeys, ProcessColoredKeys },
            { _ColoredSquares, ProcessColoredSquares },
            { _ColoredSwitches, ProcessColoredSwitches },
            { _ColorMorse, ProcessColorMorse },
            { _Coordinates, ProcessCoordinates },
            { _Corners, ProcessCorners },
            { _Crackbox, ProcessCrackbox },
            { _Creation, ProcessCreation },
            { _CrypticCycle, ProcessCrypticCycle },
            { _Cube, ProcessCube },
            { _DACHMaze, ProcessDACHMaze },
            { _DeckOfManyThings, ProcessDeckOfManyThings },
            { _DecoloredSquares, ProcessDecoloredSquares },
            { _DiscoloredSquares, ProcessDiscoloredSquares },
            { _DoubleColor, ProcessDoubleColor },
            { _DoubleOh, ProcessDoubleOh },
            { _DrDoctor, ProcessDrDoctor },
            { _ElderFuthark, ProcessElderFuthark },
            { _EncryptedEquations, ProcessEncryptedEquations },
            { _EncryptedHangman, ProcessEncryptedHangman },
            { _EncryptedMorse, ProcessEncryptedMorse },
            { _EquationsX, ProcessEquationsX },
            { _Etterna, ProcessEtterna },
            { _FactoryMaze, ProcessFactoryMaze },
            { _FastMath, ProcessFastMath },
            { _FaultyRGBMaze, ProcessFaultyRGBMaze },
            { _Flags, ProcessFlags },
            { _FlashingLights, ProcessFlashingLights },
            { _ForgetTheColors, ProcessForgetTheColors },
            { _FreeParking, ProcessFreeParking },
            { _Functions, ProcessFunctions },
            { _Gamepad, ProcessGamepad },
            { _GiantsDrink, ProcessGiantsDrink },
            { _GreenArrows, ProcessGreenArrows },
            { _GridLock, ProcessGridLock },
            { _Gryphons, ProcessGryphons },
            { _HereditaryBaseNotation, ProcessHereditaryBaseNotation },
            { _Hexabutton, ProcessHexabutton },
            { _Hexamaze, ProcessHexamaze },
            { _HexOS, ProcessHexOS },
            { _HiddenColors, ProcessHiddenColors },
            { _HillCycle, ProcessHillCycle },
            { _Hogwarts, ProcessHogwarts },
            { _HorribleMemory, ProcessHorribleMemory },
            { _HumanResources, ProcessHumanResources },
            { _Hunting, ProcessHunting },
            { _Hypercube, ProcessHypercube },
            { _IceCream, ProcessIceCream },
            { _IdentityParade, ProcessIdentityParade },
            { _Instructions, ProcessInstructions },
            { _iPhone, ProcessiPhone },
            { _JewelVault, ProcessJewelVault },
            { _JumbleCycle, ProcessJumbleCycle },
            { _Kudosudoku, ProcessKudosudoku },
            { _Lasers, ProcessLasers },
            { _LEDEncryption, ProcessLEDEncryption },
            { _LEDMath, ProcessLEDMath },
            { _LEGOs, ProcessLEGOs },
            { _Listening, ProcessListening },
            { _LogicalButtons, ProcessLogicalButtons },
            { _LogicGates, ProcessLogicGates },
            { _LondonUnderground, ProcessLondonUnderground },
            { _Mafia, ProcessMafia },
            { _Mahjong, ProcessMahjong },
            { _MaritimeFlags, ProcessMaritimeFlags },
            { _Maze, ProcessMaze },
            { _Maze3, ProcessMaze3 },
            { _Mazematics, ProcessMazematics },
            { _MazeScrambler, ProcessMazeScrambler },
            { _MegaMan2, ProcessMegaMan2 },
            { _MelodySequencer, ProcessMelodySequencer },
            { _MemorableButtons, ProcessMemorableButtons },
            { _Memory, ProcessMemory },
            { _Microcontroller, ProcessMicrocontroller },
            { _Minesweeper, ProcessMinesweeper },
            { _ModernCipher, ProcessModernCipher },
            { _ModuleMaze, ProcessModuleMaze },
            { _MonsplodeFight, ProcessMonsplodeFight },
            { _MonsplodeTradingCards, ProcessMonsplodeTradingCards },
            { _Moon, ProcessMoon },
            { _MorseAMaze, ProcessMorseAMaze },
            { _MorseButtons, ProcessMorseButtons },
            { _Morsematics, ProcessMorsematics },
            { _MorseWar, ProcessMorseWar },
            { _MouseInTheMaze, ProcessMouseInTheMaze },
            { _Murder, ProcessMurder },
            { _MysticSquare, ProcessMysticSquare },
            { _MysteryModule, ProcessMysteryModule },
            { _Necronomicon, ProcessNecronomicon },
            { _Neutralization, ProcessNeutralization },
            { _NandMs, ProcessNandMs },
            { _Navinums, ProcessNavinums },
            { _NotButton, ProcessNotButton },
            { _NotKeypad, ProcessNotKeypad },
            { _NotMaze, ProcessNotMaze },
            { _NotMorseCode, ProcessNotMorseCode },
            { _NotSimaze, ProcessNotSimaze },
            { _NotWhosOnFirst, ProcessNotWhosOnFirst },
            { _NumberedButtons, ProcessNumberedButtons },
            { _ObjectShows, ProcessObjectShows },
            { _OddOneOut, ProcessOddOneOut },
            { _OnlyConnect, ProcessOnlyConnect },
            { _OrangeArrows, ProcessOrangeArrows },
            { _OrderedKeys, ProcessOrderedKeys },
            { _OrientationCube, ProcessOrientationCube },
            { _Palindromes, ProcessPalindromes },
            { _PartialDerivatives, ProcessPartialDerivatives },
            { _PassportControl, ProcessPassportControl },
            { _PatternCube, ProcessPatternCube },
            { _PerspectivePegs, ProcessPerspectivePegs },
            { _Pie, ProcessPie },
            { _PigpenCycle, ProcessPigpenCycle },
            { _PlaceholderTalk, ProcessPlaceholderTalk },
            { _Planets, ProcessPlanets },
            { _PlayfairCycle, ProcessPlayfairCycle },
            { _Poetry, ProcessPoetry },
            { _PolyhedralMaze, ProcessPolyhedralMaze },
            { _Probing, ProcessProbing },
            { _PurpleArrows, ProcessPurpleArrows },
            { _Quintuples, ProcessQuintuples },
            { _RecoloredSwitches, ProcessRecoloredSwitches },
            { _RedArrows, ProcessRedArrows },
            { _Retirement, ProcessRetirement },
            { _ReverseMorse, ProcessReverseMorse },
            { _RGBMaze, ProcessRGBMaze},
            { _Rhythms, ProcessRhythms },
            { _RoleReversal, ProcessRoleReversal },
            { _Rule, ProcessRule },
            { _ScavengerHunt, ProcessScavengerHunt },
            { _SchlagDenBomb, ProcessSchlagDenBomb },
            { _SeaShells, ProcessSeaShells },
            { _Semamorse, ProcessSemamorse },
            { _ShapesBombs, ProcessShapesAndBombs },
            { _ShapeShift, ProcessShapeShift },
            { _ShellGame, ProcessShellGame },
            { _SillySlots, ProcessSillySlots },
            { _SimonSamples, ProcessSimonSamples },
            { _SimonSays, ProcessSimonSays },
            { _SimonScrambles, ProcessSimonScrambles },
            { _SimonScreams, ProcessSimonScreams },
            { _SimonSelects, ProcessSimonSelects },
            { _SimonSends, ProcessSimonSends },
            { _SimonShrieks, ProcessSimonShrieks },
            { _SimonSimons, ProcessSimonSimons },
            { _SimonSings, ProcessSimonSings },
            { _SimonSounds, ProcessSimonSounds },
            { _SimonSpeaks, ProcessSimonSpeaks },
            { _SimonsStar, ProcessSimonsStar },
            { _SimonStates, ProcessSimonStates },
            { _SimonStops, ProcessSimonStops },
            { _SimonStores, ProcessSimonStores},
            { _SkewedSlots, ProcessSkewedSlots },
            { _Skyrim, ProcessSkyrim },
            { _Snooker, ProcessSnooker },
            { _SonicTheHedgehog, ProcessSonicTheHedgehog },
            { _Sorting, ProcessSorting },
            { _Souvenir, ProcessSouvenir },
            { _Sphere, ProcessSphere },
            { _SplittingTheLoot, ProcessSplittingTheLoot },
            { _Switch, ProcessSwitch },
            { _Switches, ProcessSwitches },
            { _SymbolCycle, ProcessSymbolCycle },
            { _SymbolicCoordinates, ProcessSymbolicCoordinates },
            { _Synonyms, ProcessSynonyms },
            { _TapCode, ProcessTapCode },
            { _TashaSqueals, ProcessTashaSqueals },
            { _TenButtonColorCode, ProcessTenButtonColorCode },
            { _TextField, ProcessTextField },
            { _ThinkingWires, ProcessThinkingWires },
            { _ThirdBase, ProcessThirdBase },
            { _TicTacToe, ProcessTicTacToe },
            { _Timezone, ProcessTimezone },
            { _TransmittedMorse, ProcessTransmittedMorse },
            { _TurtleRobot, ProcessTurtleRobot },
            { _TwoBits, ProcessTwoBits },
            { _UltimateCycle, ProcessUltimateCycle },
            { _Ultracube, ProcessUltracube },
            { _UncoloredSquares, ProcessUncoloredSquares },
            { _UncoloredSwitches, ProcessUncoloredSwitches },
            { _UnfairCipher, ProcessUnfairCipher },
            { _UnownCipher, ProcessUnownCipher },
            { _USAMaze, ProcessUSAMaze },
            { _VaricoloredSquares, ProcessVaricoloredSquares },
            {  _Vcrcs, ProcessVcrcs },
            { _Vectors, ProcessVectors },
            { _Vexillology, ProcessVexillology },
            { _VisualImpairment, ProcessVisualImpairment },
            { _Wavetapping, ProcessWavetapping },
            { _WhosOnFirst, ProcessWhosOnFirst },
            { _Wire, ProcessWire },
            { _WireOrdering, ProcessWireOrdering },
            { _WireSequence, ProcessWireSequence },
            { _Yahtzee, ProcessYahtzee },
            { _YellowArrows, ProcessYellowArrows },
            { _Zoni, ProcessZoni }
        };

        if (!string.IsNullOrEmpty(ModSettings.SettingsPath))
        {
            bool rewriteFile;
            try
            {
                config = JsonConvert.DeserializeObject<Config>(ModSettings.Settings);
                if (config != null)
                {
                    var dictionary = JsonConvert.DeserializeObject<IDictionary<string, object>>(ModSettings.Settings);
                    object key;
                    // Rewrite the file if any keys listed in TweaksEditorSettings are not in it.
                    rewriteFile = ((List<Dictionary<string, object>>) Config.TweaksEditorSettings[0]["Listings"])
                        .Any(o => o.TryGetValue("Key", out key) && !dictionary.ContainsKey((string) key));
                    config.UpdateExcludedModules();
                }
                else
                {
                    config = new Config();
                    rewriteFile = true;
                }
            }
            catch (JsonSerializationException ex)
            {
                Debug.LogErrorFormat("<Souvenir #{0}> The mod settings file is invalid.", _moduleId);
                Debug.LogException(ex, this);
                config = new Config();
                rewriteFile = true;
            }
            if (rewriteFile)
            {
                using (var writer = new StreamWriter(ModSettings.SettingsPath))
                    new JsonSerializer() { Formatting = Formatting.Indented }.Serialize(writer, config);
            }
        }
        else
            config = new Config();

        var ignoredList = BossModule.GetIgnoredModules(Module, _defaultIgnoredModules);
        Debug.LogFormat(@"<Souvenir #{0}> Ignored modules: {1}", _moduleId, ignoredList.JoinString(", "));
        ignoredModules.UnionWith(ignoredList);

        Bomb.OnBombExploded += delegate { _exploded = true; StopAllCoroutines(); };
        Bomb.OnBombSolved += delegate
        {
            // This delegate gets invoked when _any_ bomb in the room is solved,
            // so we need to check if the bomb this module is on is actually solved
            if (Bomb.GetSolvedModuleNames().Count == Bomb.GetSolvableModuleNames().Count)
                StopAllCoroutines();
        };

        _attributes = typeof(Question).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => Ut.KeyValuePair((Question) f.GetValue(null), GetQuestionAttribute(f)))
            .Where(kvp => kvp.Value != null)
            .ToDictionary();

        var origRotation = SurfaceRenderer.transform.rotation;
        SurfaceRenderer.transform.eulerAngles = new Vector3(0, 180, 0);
        SurfaceSizeFactor = SurfaceRenderer.bounds.size.x / (2 * .834) * .9;
        SurfaceRenderer.transform.rotation = origRotation;

        disappear();
        WarningIcon.SetActive(false);
        SetWordWrappedText(Ut.NewArray(
            "I see dead defusers.",     // “I see dead people”, (Sixth Sense)
            "Welcome... to the real bomb.",     // “Welcome... to the real world.” (The Matrix)
            "I’m gonna make him a bomb he can’t defuse.",   // “I’m gonna make him an offer he can’t refuse.” (The Godfather)
            "Defuse it again, Sam.",    // “Play it again, Sam.” (Casablanca) (misquote)
            "Louis, I think this is the beginning of a beautiful explosion.",   // “Louis, I think this is the beginning of a beautiful friendship.” (Casablanca)
            "Here’s looking at you, defuser.",  // “Here’s looking at you, kid.” (Casablanca)
            "Hey. I could defuse this bomb in ten seconds flat.",   // “Hey. I could clear the sky in ten seconds flat.” (MLP:FiM, Friendship is Magic - Part 1)
            "Go ahead, solve my bomb.", // “Go ahead, make my day.” (Sudden Impact / Dirty Harry series)
            "May the bomb be with you.",    // “May the Force be with you.” (Star Wars IV: A New Hope)
            "I love the smell of explosions in the morning.",   // “I love the smell of napalm in the morning.” (Apocalypse Now)
            "Blowing up means never having to say you’re sorry.",   // “Love means never having to say you're sorry.” (Love Story)
            "The stuff that bombs are made of.",    // “The Stuff That Dreams Are Made Of” (“Coming Around Again” album by Carly Simon)
            "E.T. defuse bomb.",    // “E.T. phone home.” (E.T. the Extra-Terrestrial)
            "Bomb. James Bomb.",    // “Bond. James Bond.” (Dr. No / James Bond series)
            "You can’t handle the bomb!",   // “You can’t handle the truth!” (A Few Good Men)
            "Blow up the usual suspects.",  // “Round up the usual suspects.” (Casablanca)
            "You’re gonna need a bigger bomb.", // “You’re gonna need a bigger boat.” (Jaws)
            "Bombs are like a box of chocolates. You never know what you’re gonna get.",    // “My mom always said life was like a box of chocolates. You never know what you're gonna get.” (Forrest Gump)
            "Houston, we have a module.",   // “Houston, we have a problem.” (Apollo 13)
            "Elementary, my dear expert.",  // “Elementary, my dear Watson.” (Sherlock Holmes) (misquote)
            "Forget it, Jake, it’s KTANE.",     // “Forget it, Jake, it’s Chinatown.” (Chinatown)
            "I have always depended on the fitness of experts.",    // “I’ve always depended on the kindness of strangers.” (A Streetcar Named Desire)
            "A bomb. Exploded, not defused.",   // “A Martini. Shaken, not stirred.” (Diamonds Are Forever (novel) / James Bond)
            "I’m the king of the bomb!",    // “I’m the king of the world!” (Titanic)
            "Blow me up, Scotty.",  // “Beam me up, Scotty!” (Star Trek misquote)
            "Yabba dabba boom!",    // “Yabba dabba doo!” (Flintstones)
            "This bomb will self-destruct in five seconds.",    // “This tape will self-destruct in five seconds.” (Mission: Impossible)
            "Defusing is futile.",  // “Resistance is futile.” (Star Trek: The Next Generation)
            "Is that your final answer?",   // direct quote (Who Wants to be a Millionaire?)
            "A bomb’s best friend is his defuser.", // “A man’s best friend is his dog.” (attorney George Graham Vest, 1870 Warrensburg)
            "Keep your experts close, but your bomb closer.",   // “Keep your friends close and your enemies closer.” (The Prince / Machiavelli)
            "Fasten your seatbelts. It’s going to be a bomby night.",   // “Fasten your seat belts, it’s going to be a bumpy night.” (All About Eve)
            "Show me the modules!", // “Show me the money!” (Jerry Maguire)
            "We’ll always have batteries.", // “We’ll always have Paris.” (Casablanca)
            "Say hello to my little bomb.", // “Say hello to my little friend!” (Scarface)
            "You’re a defuser, Harry.", // “You’re a wizard, Harry.” (Harry Potter and the Philosopher’s Stone)
            "I’m sorry, Dave. I’m afraid I can’t defuse that.", // “I’m sorry, Dave. I’m afraid I can’t do that.” (2001: A Space Odyssey)
            "You either die a defuser, or you live long enough to see yourself become the expert.", // “Well, I guess you either die a hero or you live long enough to see yourself become the villain.” (The Dark Knight)
            "This isn’t defusing. This is exploding... with style.",    // “This isn’t flying. This is falling... with style.” (Toy Story)
            "Could you describe the module, sir?",  // “Could you describe the ruckus, sir?” (The Breakfast Club)
            "You want widgets? I got twenty.",  // “You want thingamabobs? I got twenty.” (The Little Mermaid)
            "We don’t need no stinking widgets.",   // “We don’t need no stinking badges!” (The Treasure of the Sierra Madre)
            "Say edgework one more goddamn time.",  // “Say what one more goddamn time.” (Pulp Fiction)
            "How do you like them modules?",    // “How do you like them apples?” (Good Will Hunting)
            "Introducing: The Double... Decker... Bomb!",   // “Introducing: The Double... Decker... Couch!” (The LEGO Movie)
            "Have you got your wires crossed?", // “Have you got your lions crossed?” (The Lion King)
            "Don’t cross the wires.",   // “Don’t cross the streams.” (Ghostbusters)
            "Wanna hear the most annoying explosion in the world?", // “Wanna hear the most annoying sound in the world?” (Dumb & Dumber)
            "Manuals? Where we’re going, we don’t need manuals.",   // “Roads? Where we’re going, we don’t need roads.” (Back to the Future)
            "On a long enough time line, the survival rate for everyone will drop to zero.", // direct quote (Fight Club (novel))
            "This is your bomb, and it’s ending one minute at a time.", // “This is your life and it’s ending one minute at a time.” (Fight Club)
            "The first rule of defusal is, you keep talking about defusal.",    // “The first rule of Fight Club is, you don’t talk about Fight Club.” (Fight Club)
            "Well, here’s another nice mess you’ve gotten me into!",     // direct quote (Sons of the Desert / Oliver Hardy)
            "You know how to defuse, don’t you, Steve? You just put your wires together and cut.",  // “You know how to whistle, don’t you Steve? You just put your lips together, and blow.” (To Have And Have Not)
            "Mrs. Defuser, you’re trying to disarm me. Aren’t you?",    // “Mrs. Robinson, you’re trying to seduce me. Aren’t you?” (The Graduate)
            "We defuse bombs.",  // “We rob banks.“ (Bonnie and Clyde)
            "Somebody set up us the bomb.",  // direct quote (Zero Wing)
            "Luke, I am your expert.", // “Luke, I am your father.“ (Star Wars V: The Empire Strikes Back) (misquote)
            "Everybody knows that the best way to learn is under intense life threatening crisis.", // direct quote (Spider-Man: Into the Spider-Verse)
            "It needs to be about 20 percent more exploded.", // “It needs to be about 20 percent cooler.” (MLP:FiM, Suited for Success)
            "I am a bomb. What’s your excuse?", // “I am a child. What’s your excuse?” (Steven Universe, Change your Mind)
            "The same thing we do every time, expert. Try to defuse the bomb!", // “The same thing we do every time, Pinky. Try to take over the world!” (Pinky and the Brain)
            "Anybody order fried defuser?", // “Anybody order fried sauerkraut?” (Once Upon a Time in Hollywood)
            "I’ve got some defusers I need to blow into smithereens!", // “I’ve got some children I need to make into corpses!” (Gravity Falls, Weirdmageddon 3: Take Back The Falls)
            "I imagine blowing up so much it feels more like a memory.", // “I imagine death so much it feels more like a memory.” (Hamilton)
            "I am inevitable.", // direct quote (Avengers: Endgame)
            "Dread it, run from it, bombs still explode.", // “Dread it, run from it, destiny still arrives.” (Avengers: Infinity War)
            "In time, you will know what it’s like to lose. To feel so desperately that you’re right, yet to strike all the same.", // “In time, you will know what it’s like to lose. To feel so desperately that you’re right, yet to fail all the same.” (Avengers: Infinity War)
            "Defuser, I’ve come to bargain.", // “Dormammu, I’ve come to bargain.” (Doctor Strange)
            "I can do this all day.", // direct quote (Captain America: Civil War)
            "There... are... FOUR! BOMBS!!!", // “There... are... FOUR! LIGHTS!!!” (Star Trek TNG, Chain of Command)
            "It’s a beautiful thing, the detonation of bombs." // “It’s a beautiful thing, the destruction of words.” (1984)

        ).PickRandom(), 1.75);

        if (transform.parent != null)
        {
            FieldInfo<object> fldType = null;
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                var gameObject = transform.parent.GetChild(i).gameObject;
                var module = gameObject.GetComponent<KMBombModule>();
                if (module != null)
                {
                    if ((config.ExcludeIgnoredModules && ignoredModules.Contains(module.ModuleDisplayName)) || config.ExcludedModules.Contains(module.ModuleType))
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning {1} because it is excluded in the mod settings.", _moduleId, module.ModuleDisplayName);
                    }
                    else
                    {
                        StartCoroutine(ProcessModule(module));
                    }
                }
                else if (!config.ExcludeVanillaModules)
                {
                    var vanillaModule = transform.parent.GetChild(i).gameObject.GetComponent("BombComponent");
                    if (vanillaModule != null)
                    {
                        // For vanilla modules, we will attach a temporary KMBombModule component to the module.
                        // We'll remove it after the coroutine starts.
                        // The routine will already have a reference to the actual BombComponent by then.
                        if (fldType == null) fldType = GetField<object>(vanillaModule.GetType(), "ComponentType", true);
                        if (fldType == null) continue;
                        var typeCode = (int) fldType.GetFrom(vanillaModule);
                        string type; string displayName;
                        switch (typeCode)
                        {
                            case 3: type = "BigButton"; displayName = "The Button"; break;
                            case 5: type = "Simon"; displayName = "Simon Says"; break;
                            case 6: type = "WhosOnFirst"; displayName = "Who's on First"; break;
                            case 7: type = "Memory"; displayName = "Memory"; break;
                            case 10: type = "WireSequence"; displayName = "Wire Sequence"; break;
                            case 11: type = "Maze"; displayName = "Maze"; break;
                            default: continue;  // Other components are not supported modules.
                        }
                        module = gameObject.AddComponent<KMBombModule>();
                        module.ModuleType = type;
                        module.ModuleDisplayName = displayName;
                        StartCoroutine(ProcessModule(module));
                        Destroy(module);
                    }
                }
            }
        }

        _isActivated = false;
        Module.OnActivate += delegate
        {
            _isActivated = true;
            if (Application.isEditor && !ModulePresent)
            {
                // Testing in Unity
                foreach (var entry in _attributes)
                {
                    if (entry.Value.Type != AnswerType.Sprites && (entry.Value.AllAnswers == null || entry.Value.AllAnswers.Length == 0) &&
                        (entry.Value.ExampleAnswers == null || entry.Value.ExampleAnswers.Length == 0) && entry.Value.AnswerGenerator == null)
                    {
                        Debug.LogWarningFormat("<Souvenir #{0}> Question {1} has no answers. You should specify either SouvenirQuestionAttribute.AllAnswers or SouvenirQuestionAttribute.ExampleAnswers (with preferredWrongAnswers in-game), or add an AnswerGeneratorAttribute to the question enum value.", _moduleId, entry.Key);
                        WarningIcon.SetActive(true);
                    }
                }
                StartCoroutine(TestModeCoroutine());
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
    }

    private static SouvenirQuestionAttribute GetQuestionAttribute(FieldInfo field)
    {
        var attribute = field.GetCustomAttribute<SouvenirQuestionAttribute>();
        if (attribute != null)
            attribute.AnswerGenerator = field.GetCustomAttribute<AnswerGeneratorAttribute>();
        return attribute;
    }

    private IEnumerator TestModeCoroutine()
    {
        Debug.LogFormat(this, "<Souvenir #{0}> Entering Unity testing mode. To select a question, set SouvenirModule.TestQuestion and click on the game view.", _moduleId);
        var questions = Ut.GetEnumValues<Question>();
        var curQuestion = 0;
        var curOrd = 0;
        var curExample = 0;
        Action showQuestion = () =>
        {
            SouvenirQuestionAttribute attr;
            if (!_attributes.TryGetValue(questions[curQuestion], out attr))
            {
                Debug.LogErrorFormat("<Souvenir #{1}> Error: Question {0} has no attribute.", questions[curQuestion], _moduleId);
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
                switch (attr.Type)
                {
                    case AnswerType.Sprites:
                        var answerSprites = attr.SpriteField == null ? ExampleSprites : (Sprite[]) typeof(SouvenirModule).GetField(attr.SpriteField, BindingFlags.Instance | BindingFlags.Public).GetValue(this) ?? ExampleSprites;
                        if (answerSprites != null)
                            answerSprites.Shuffle();
                        SetQuestion(new QandASprite(
                            module: attr.ModuleNameWithThe,
                            question: string.Format(attr.QuestionText, fmt),
                            correct: 0,
                            answers: answerSprites));
                        break;

                    default:
                        var answers = new List<string>(attr.NumAnswers);
                        if (attr.AllAnswers != null) answers.AddRange(attr.AllAnswers);
                        else if (attr.ExampleAnswers != null) answers.AddRange(attr.ExampleAnswers);
                        if (answers.Count <= attr.NumAnswers)
                        {
                            if (attr.AnswerGenerator != null)
                                answers.AddRange(attr.AnswerGenerator.GetAnswers(this).Except(answers).Distinct().Take(attr.NumAnswers - answers.Count));
                            answers.Shuffle();
                        }
                        else
                        {
                            answers.Shuffle();
                            answers.RemoveRange(attr.NumAnswers, answers.Count - attr.NumAnswers);
                        }
                        SetQuestion(new QandAText(
                            module: attr.ModuleNameWithThe,
                            question: string.Format(attr.QuestionText, fmt),
                            correct: 0,
                            answers: answers.ToArray(),
                            font: Fonts[attr.Type == AnswerType.DynamicFont ? 0 : (int) attr.Type],
                            fontTexture: FontTextures[attr.Type == AnswerType.DynamicFont ? 0 : (int) attr.Type],
                            fontMaterial: FontMaterial,
                            layout: attr.Layout));
                        break;
                }
            }
            catch (FormatException e)
            {
                Debug.LogErrorFormat("<Souvenir #{3}> FormatException {0}\nQuestionText={1}\nfmt=[{2}]", e.Message, attr.QuestionText, fmt.JoinString(", ", "\"", "\""), _moduleId);
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

        if (TwitchPlaysActive)
            ActivateTwitchPlaysNumbers();

        while (true)
        {
            if (TestQuestion != null && Application.isFocused)
            {
                TestQuestion = TestQuestion.Trim();
                if (TestQuestion.Length > 0)
                {
                    var i = questions.IndexOf(q => q.ToString().StartsWith(TestQuestion, StringComparison.InvariantCultureIgnoreCase));
                    if (i < 0)
                        Debug.LogFormat(this, "<Souvenir #{0}> No question matching '{1}' was found.", _moduleId, TestQuestion);
                    else
                    {
                        curQuestion = i;
                        curExample = 0;
                        curOrd = 0;
                        showQuestion();
                    }
                }
                TestQuestion = null;
            }
            yield return null;
        }
    }

    void setAnswerHandler(int index, Action<int> handler)
    {
        Answers[index].OnInteract = delegate
        {
            Answers[index].AddInteractionPunch();
            handler(index);
            return false;
        };
    }

    private void disappear()
    {
        TextMesh.gameObject.SetActive(false);
        AnswersParent.SetActive(false);
    }

    private void HandleAnswer(int index)
    {
        if (_animating || _isSolved)
            return;

        if (_currentQuestion == null || index >= _currentQuestion.NumAnswers)
            return;

        Debug.LogFormat("[Souvenir #{0}] Clicked answer #{1} ({2}). {3}.", _moduleId, index + 1, _currentQuestion.DebugAnswers.Skip(index).First(), _currentQuestion.CorrectIndex == index ? "Correct" : "Wrong");

        if (_currentQuestion.CorrectIndex == index)
        {
            StartCoroutine(CorrectAnswer());
        }
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

    private IEnumerator CorrectAnswer()
    {
        _animating = true;
        Audio.PlaySoundAtTransform("Answer", transform);
        dismissQuestion();
        yield return new WaitForSeconds(.5f);
        _animating = false;
    }

    private void dismissQuestion()
    {
        _currentQuestion = null;
        disappear();
    }

    private IEnumerator revealThenMoveOn()
    {
        var on = false;
        for (int i = 0; i < 14; i++)
        {
            _currentQuestion.BlinkCorrectAnswer(on, this);
            on = !on;
            yield return new WaitForSeconds(.1f);
        }

        dismissQuestion();
        _animating = false;
    }

    private IEnumerator Play()
    {
        if (TwitchPlaysActive)
            ActivateTwitchPlaysNumbers();

        var numPlayableModules = Bomb.GetSolvableModuleNames().Count(x => !ignoredModules.Contains(x));

        while (true)
        {
            // A module handler can increment this value temporarily to delay asking questions. (Currently only the Mystery Module handler does this when Souvenir is hidden by a Mystery Module.)
            while (_avoidQuestions > 0)
                yield return new WaitForSeconds(.1f);

            var numSolved = Bomb.GetSolvedModuleNames().Count(x => !ignoredModules.Contains(x));
            if (_questions.Count == 0 && (numSolved >= numPlayableModules || _coroutinesActive == 0))
            {
                // Very rare case: another coroutine could still be waiting to detect that a module is solved and then add another question to the queue
                yield return new WaitForSeconds(.1f);

                // If still no new questions, all supported modules are solved and we’re done. (Or maybe a coroutine is stuck in a loop, but then it’s bugged and we need to cancel it anyway.)
                if (_questions.Count == 0)
                    break;
            }

            IEnumerable<QuestionBatch> eligible = _questions;

            // If we reached the end of the bomb, everything is eligible.
            if (numSolved < numPlayableModules)
                // Otherwise, make sure there has been another solved module since
                eligible = eligible.Where(e => e.NumSolved < numSolved);

            var numEligibles = eligible.Count();

            if ((numSolved < numPlayableModules && numEligibles < 3) || numEligibles == 0)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            var batch = eligible.PickRandom();
            _questions.Remove(batch);
            if (batch.Questions.Length == 0)
                continue;

            SetQuestion(batch.Questions.PickRandom());
            while (_currentQuestion != null || _animating)
                yield return new WaitForSeconds(.5f);
        }

        Debug.LogFormat("[Souvenir #{0}] Questions exhausted. Module solved.", _moduleId);
        _isSolved = true;
        Module.HandlePass();
    }

    private void SetQuestion(QandA q)
    {
        Debug.LogFormat("[Souvenir #{0}] Asking question: {1}", _moduleId, q.DebugString);
        _currentQuestion = q;
        SetWordWrappedText(q.QuestionText, q.DesiredHeightFactor);
        q.SetAnswers(this);
        AnswersParent.SetActive(true);
        Audio.PlaySoundAtTransform("Question", transform);
    }

    private static readonly double[][] _acceptableWidths = Ut.NewArray(
        // First value is y (vertical text advancement), second value is width of the Surface mesh at this y
        new[] { 0.834 - 0.834, 0.834 + 0.3556 },
        new[] { 0.834 - 0.7628, 0.834 + 0.424 },
        new[] { 0.834 - 0.6864, 0.834 + 0.424 },
        new[] { 0.834 - 0.528, 0.834 + 0.5102 },
        new[] { 0.834 - 0.4452, 0.834 + 0.6618 },
        new[] { 0.834 - 0.4452, 0.834 + 0.7745 },
        new[] { 0.834 - 0.391, 0.834 + 0.834 }
    );

    private void SetWordWrappedText(string text, double desiredHeightFactor)
    {
        var low = 1;
        var high = 256;
        var desiredHeight = desiredHeightFactor * SurfaceSizeFactor;
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
                    var y = line * heightOfALine / SurfaceSizeFactor;
                    if (line < wrapWidths.Count)
                        return wrapWidths[line];
                    while (wrapWidths.Count < line)
                        wrapWidths.Add(0);
                    var i = 1;
                    while (i < _acceptableWidths.Length && _acceptableWidths[i][0] < y)
                        i++;
                    if (i == _acceptableWidths.Length)
                        wrapWidths.Add(_acceptableWidths[i - 1][1] * SurfaceSizeFactor);
                    else
                    {
                        var lambda = (y - _acceptableWidths[i - 1][0]) / (_acceptableWidths[i][0] - _acceptableWidths[i - 1][0]);
                        wrapWidths.Add((_acceptableWidths[i - 1][1] * (1 - lambda) + _acceptableWidths[i][1] * lambda) * SurfaceSizeFactor);
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

    private IEnumerator ProcessModule(KMBombModule module)
    {
        _coroutinesActive++;
        var moduleType = module.ModuleType;
        _moduleCounts.IncSafe(moduleType);
        var iterator = _moduleProcessors.Get(moduleType, null);

        if (iterator != null)
        {
            supportedModuleNames.Add(module.ModuleDisplayName);
            yield return null;  // Ensures that the module’s Start() method has run
            Debug.LogFormat("<Souvenir #{1}> Module {0}: Start processing.", moduleType, _moduleId);

            // I’d much rather just put a ‘foreach’ loop inside a ‘try’ block, but Unity’s C# version doesn’t allow ‘yield return’ inside of ‘try’ blocks yet
            using (var e = iterator(module).GetEnumerator())
            {
                while (true)
                {
                    bool canMoveNext;
                    try { canMoveNext = e.MoveNext(); }
                    catch (AbandonModuleException ex)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning {1} because: {2}", _moduleId, module.ModuleDisplayName, ex.Message);
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogFormat("<Souvenir #{0}> The {1} handler threw an exception ({2}):\n{3}", _moduleId, module.ModuleDisplayName, ex.GetType().FullName, ex.StackTrace);
                        yield break;
                    }
                    if (TwitchAbandonModule.Contains(module))
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning {1} because Twitch Plays told me to.", _moduleId, module.ModuleDisplayName);
                        yield break;
                    }
                    if (!canMoveNext)
                        break;
                    yield return e.Current;
                }
            }

            if (!_legitimatelyNoQuestions.Contains(module) && !_questions.Any(q => q.Module == module))
            {
                Debug.LogFormat("[Souvenir #{0}] There was no question generated for {1}. Please report this to Andrio or the implementer for that module as this may indicate a bug in Souvenir. Remember to send them this logfile.", _moduleId, module.ModuleDisplayName);
                WarningIcon.SetActive(true);
            }
            Debug.LogFormat("<Souvenir #{1}> Module {0}: Finished processing.", moduleType, _moduleId);
        }
        else
        {
            Debug.LogFormat("<Souvenir #{1}> Module {0}: Not supported.", moduleType, _moduleId);
        }

        _coroutinesActive--;
    }
    #endregion

    #region Helper methods for Reflection (used by module handlers)
    private Component GetComponent(KMBombModule module, string name)
    {
        return GetComponent(module.gameObject, name);
    }
    private Component GetComponent(GameObject module, string name)
    {
        var comp = module.GetComponent(name);
        if (comp == null)
        {
            comp = module.GetComponents(typeof(Component)).FirstOrDefault(c => c.GetType().FullName == name);
            if (comp == null)
                throw new AbandonModuleException("{0} game object has no {1} component. Components are: {2}", module.name, name, module.GetComponents(typeof(Component)).Select(c => c.GetType().FullName).JoinString(", "));
        }
        return comp;
    }

    private FieldInfo<T> GetField<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {1} field {0} of type {2} from a null object.", name, isPublic ? "public" : "non-public", typeof(T).FullName);
        return new FieldInfo<T>(target, GetFieldImpl<T>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private FieldInfo<T> GetField<T>(Type targetType, string name, bool isPublic = false, bool noThrow = false)
    {
        if (targetType == null && !noThrow)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type {2} from a null type.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new FieldInfo<T>(null, GetFieldImpl<T>(targetType, name, isPublic, BindingFlags.Instance, noThrow));
    }

    private IntFieldInfo GetIntField(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type int from a null object.", isPublic ? "public" : "non-public", name);
        return new IntFieldInfo(target, GetFieldImpl<int>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private ArrayFieldInfo<T> GetArrayField<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type {2}[] from a null object.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new ArrayFieldInfo<T>(target, GetFieldImpl<T[]>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private ListFieldInfo<T> GetListField<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type List<{2}> from a null object.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new ListFieldInfo<T>(target, GetFieldImpl<List<T>>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private FieldInfo<T> GetStaticField<T>(Type targetType, string name, bool isPublic = false)
    {
        if (targetType == null)
            throw new AbandonModuleException("Attempt to get {0} static field {1} of type {2} from a null type.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new FieldInfo<T>(null, GetFieldImpl<T>(targetType, name, isPublic, BindingFlags.Static));
    }

    private FieldInfo GetFieldImpl<T>(Type targetType, string name, bool isPublic, BindingFlags bindingFlags, bool noThrow = false)
    {
        FieldInfo fld; Type type = targetType;
        while (type != null && type != typeof(object))
        {
            fld = type.GetField(name, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | bindingFlags);
            if (fld != null)
                goto found;

            // In case it’s actually an auto-implemented property and not a field.
            fld = type.GetField("<" + name + ">k__BackingField", BindingFlags.NonPublic | bindingFlags);
            if (fld != null)
                goto found;

            // Reflection won’t return private fields in base classes unless we check those explicitly
            type = type.BaseType;
        }

        if (noThrow)
            return null;
        throw new AbandonModuleException("Type {0} does not contain {1} field {2}. Fields are: {3}", targetType, isPublic ? "public" : "non-public", name,
            targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(f => string.Format("{0} {1} {2}", f.IsPublic ? "public" : "private", f.FieldType.FullName, f.Name)).JoinString(", "));

        found:
        if (!typeof(T).IsAssignableFrom(fld.FieldType))
        {
            if (noThrow)
                return null;
            throw new AbandonModuleException("Type {0} has {1} field {2} of type {3} but expected type {4}.", targetType, isPublic ? "public" : "non-public", name, fld.FieldType.FullName, typeof(T).FullName);
        }
        return fld;
    }

    private MethodInfo<T> GetMethod<T>(object target, string name, int numParameters, bool isPublic = false)
    {
        return GetMethodImpl<T>(typeof(T), target, name, numParameters, isPublic);
    }

    private MethodInfo<object> GetMethod(object target, string name, int numParameters, bool isPublic = false)
    {
        return GetMethodImpl<object>(typeof(void), target, name, numParameters, isPublic);
    }

    private MethodInfo<T> GetMethodImpl<T>(Type returnType, object target, string name, int numParameters, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {1} method {0} of return type {2} from a null object.", name, isPublic ? "public" : "non-public", returnType.FullName);

        var bindingFlags = (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | BindingFlags.Instance;
        var targetType = target.GetType();
        var mths = targetType.GetMethods(bindingFlags).Where(m => m.Name == name && m.GetParameters().Length == numParameters && returnType.IsAssignableFrom(m.ReturnType)).Take(2).ToArray();
        if (mths.Length == 0)
            throw new AbandonModuleException("Type {0} does not contain {1} method {2} with return type {3} and {4} parameters.", targetType, isPublic ? "public" : "non-public", name, returnType.FullName, numParameters);
        if (mths.Length > 1)
            throw new AbandonModuleException("Type {0} contains multiple {1} methods {2} with return type {3} and {4} parameters.", targetType, isPublic ? "public" : "non-public", name, returnType.FullName, numParameters);
        return new MethodInfo<T>(target, mths[0]);
    }

    private PropertyInfo<T> GetProperty<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {1} property {0} of type {2} from a null object.", name, isPublic ? "public" : "non-public", typeof(T).FullName);
        return GetPropertyImpl<T>(target, target.GetType(), name, isPublic, BindingFlags.Instance);
    }

    private PropertyInfo<T> GetStaticProperty<T>(Type targetType, string name, bool isPublic = false)
    {
        if (targetType == null)
            throw new AbandonModuleException("Attempt to get {0} static property {1} of type {2} from a null type.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return GetPropertyImpl<T>(null, targetType, name, isPublic, BindingFlags.Static);
    }

    private PropertyInfo<T> GetPropertyImpl<T>(object target, Type targetType, string name, bool isPublic, BindingFlags bindingFlags)
    {
        var fld = targetType.GetProperty(name, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | bindingFlags);
        if (fld == null)
            throw new AbandonModuleException("Type {0} does not contain {1} property {2}. Properties are: {3}", targetType, isPublic ? "public" : "non-public", name,
                targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(f => string.Format("{0} {1} {2}", f.GetGetMethod().IsPublic ? "public" : "private", f.PropertyType.FullName, f.Name)).JoinString(", "));
        if (!typeof(T).IsAssignableFrom(fld.PropertyType))
            throw new AbandonModuleException("Type {0} has {1} field {2} of type {3} but expected type {4}.", targetType, isPublic ? "public" : "non-public", name, fld.PropertyType.FullName, typeof(T).FullName, _moduleId);
        return new PropertyInfo<T>(target, fld);
    }
    #endregion

    #region Other helper methods (used by module handlers)
    private void addQuestion(KMBombModule module, Question question, string[] formatArguments = null, string[] correctAnswers = null, string[] preferredWrongAnswers = null)
    {
        addQuestions(module, makeQuestion(question, module.ModuleType, formatArguments, correctAnswers, preferredWrongAnswers));
    }

    private void addQuestion(KMBombModule module, Question question, string[] formatArguments = null, Sprite[] correctAnswers = null, Sprite[] preferredWrongAnswers = null)
    {
        addQuestions(module, makeQuestion(question, module.ModuleType, formatArguments, correctAnswers, preferredWrongAnswers));
    }

    private void addQuestions(KMBombModule module, IEnumerable<QandA> questions)
    {
        var qs = questions.Where(q => q != null).ToArray();
        if (qs.Length == 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Empty question batch provided for {1}.", _moduleId, module.ModuleDisplayName);
            return;
        }
        Debug.LogFormat("<Souvenir #{0}> Adding question batch:\n{1}", _moduleId, qs.Select(q => "    • " + q.DebugString).JoinString("\n"));
        _questions.Add(new QuestionBatch
        {
            NumSolved = Bomb.GetSolvedModuleNames().Count,
            Questions = qs,
            Module = module
        });
    }

    private void addQuestions(KMBombModule module, params QandA[] questions)
    {
        addQuestions(module, (IEnumerable<QandA>) questions);
    }

    private string titleCase(string str)
    {
        return str.Length < 1 ? str : char.ToUpperInvariant(str[0]) + str.Substring(1).ToLowerInvariant();
    }

    private QandA makeQuestion(Question question, string moduleKey, string[] formatArgs = null, string[] correctAnswers = null, string[] preferredWrongAnswers = null)
    {
        return makeQuestion(question, moduleKey,
            (attr, q, correct, answers) =>
            {
                if (attr.Type == AnswerType.DynamicFont || attr.Type == AnswerType.Sprites)
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> The module handler for {1} attempted to output a question that requires a sprite or dynamic font, but didn’t supply one.", _moduleId, moduleKey);
                    throw new InvalidOperationException();
                }
                return new QandAText(attr.ModuleNameWithThe, q, correct, answers.ToArray(), Fonts[(int) attr.Type], FontTextures[(int) attr.Type], FontMaterial, attr.Layout);
            },
            formatArgs, correctAnswers, preferredWrongAnswers);
    }

    private QandA makeQuestion(Question question, string moduleKey, Font font, Texture fontTexture, string[] formatArgs = null, string[] correctAnswers = null, string[] preferredWrongAnswers = null)
    {
        return makeQuestion(question, moduleKey,
            (attr, q, correct, answers) =>
            {
                if (attr.Type != AnswerType.DynamicFont)
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> The module handler for {1} attempted to use a dynamic font but the corresponding question is not marked as AnswerType.DynamicFont.", _moduleId, moduleKey);
                    throw new InvalidOperationException();
                }
                return new QandAText(attr.ModuleNameWithThe, q, correct, answers.ToArray(), font, fontTexture, FontMaterial, attr.Layout);
            },
            formatArgs, correctAnswers, preferredWrongAnswers);
    }

    private QandA makeQuestion(Question question, string moduleKey, string[] formatArgs = null, Sprite[] correctAnswers = null, Sprite[] preferredWrongAnswers = null)
    {
        return makeQuestion(question, moduleKey,
            (attr, q, correct, answers) =>
            {
                if (attr.Type != AnswerType.Sprites)
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> The module handler for {1} attempted to use a sprite but the corresponding question is not marked as AnswerType.Sprite.", _moduleId, moduleKey);
                    throw new InvalidOperationException();
                }
                return new QandASprite(attr.ModuleNameWithThe, q, correct, answers.ToArray());
            },
            formatArgs, correctAnswers, preferredWrongAnswers);
    }

    private QandA makeQuestion<T>(Question question, string moduleKey, Func<SouvenirQuestionAttribute, string, int, T[], QandA> questionConstructor, string[] formatArgs = null, T[] correctAnswers = null, T[] preferredWrongAnswers = null)
    {
        SouvenirQuestionAttribute attr;
        if (!_attributes.TryGetValue(question, out attr))
        {
            Debug.LogErrorFormat("<Souvenir #{1}> Question {0} has no SouvenirQuestionAttribute.", question, _moduleId);
            return null;
        }

        var allAnswers = attr.AllAnswers as T[];
        if (allAnswers != null)
        {
            var inconsistency = correctAnswers.Except(allAnswers).FirstOrDefault();
            if (inconsistency != null)
            {
                Debug.LogErrorFormat("<Souvenir #{2}> Question {0}: invalid answer: {1}.", question, inconsistency.ToString() ?? "<null>", _moduleId);
                return null;
            }
            if (preferredWrongAnswers != null)
            {
                var inconsistency2 = preferredWrongAnswers.Except(allAnswers).FirstOrDefault();
                if (inconsistency2 != null)
                {
                    Debug.LogErrorFormat("<Souvenir #{2}> Question {0}: invalid preferred wrong answer: {1}.", question, inconsistency2.ToString() ?? "<null>", _moduleId);
                    return null;
                }
            }
        }

        var answers = new List<T>(attr.NumAnswers);
        if (allAnswers == null && attr.AnswerGenerator == null)
        {
            if (preferredWrongAnswers == null || preferredWrongAnswers.Length == 0)
            {
                Debug.LogErrorFormat("<Souvenir #{0}> Question {1} has no answers. You must specify either the full set of possible answers in SouvenirQuestionAttribute.AllAnswers, provide possible wrong answers through the preferredWrongAnswers parameter, or add an AnswerGeneratorAttribute to the question enum value.", _moduleId, question);
                return null;
            }
            answers.AddRange(preferredWrongAnswers.Except(correctAnswers).Distinct());
        }
        else
        {
            // Pick 𝑛−1 random wrong answers.
            if (allAnswers != null) answers.AddRange(allAnswers.Except(correctAnswers));
            if (answers.Count <= attr.NumAnswers - 1)
            {
                if (attr.AnswerGenerator != null && typeof(T) == typeof(string))
                    answers.AddRange(attr.AnswerGenerator.GetAnswers(this).Except(answers.Concat(correctAnswers) as IEnumerable<string>).Distinct().Take(attr.NumAnswers - 1 - answers.Count) as IEnumerable<T>);
                if (answers.Count == 0 && (preferredWrongAnswers == null || preferredWrongAnswers.Length == 0))
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> Question {1}'s answer generator did not generate any answers.", _moduleId, question);
                    return null;
                }
            }
            else
            {
                answers.Shuffle();
                answers.RemoveRange(attr.NumAnswers - 1, answers.Count - (attr.NumAnswers - 1));
            }
            // Add the preferred wrong answers, if any. If we had added them earlier, they’d come up too rarely.
            if (preferredWrongAnswers != null)
                answers.AddRange(preferredWrongAnswers.Except(answers.Concat(correctAnswers)).Distinct());
        }
        answers.Shuffle();
        if (answers.Count >= attr.NumAnswers) answers.RemoveRange(attr.NumAnswers - 1, answers.Count - (attr.NumAnswers - 1));

        var correctIndex = Rnd.Range(0, answers.Count + 1);
        answers.Insert(correctIndex, correctAnswers.PickRandom());

        var numSolved = _modulesSolved.Get(moduleKey);
        if (numSolved < 1)
        {
            Debug.LogErrorFormat("<Souvenir #{0}> Abandoning {1} ({2}) because you forgot to increment the solve count.", _moduleId, attr.ModuleName, moduleKey);
            return null;
        }

        var allFormatArgs = new string[formatArgs != null ? formatArgs.Length + 1 : 1];
        allFormatArgs[0] = _moduleCounts.Get(moduleKey) > 1
            ? string.Format("the {0} you solved {1}", attr.ModuleName, ordinal(numSolved))
            : attr.AddThe ? "The\u00a0" + attr.ModuleName : attr.ModuleName;
        if (formatArgs != null)
            Array.Copy(formatArgs, 0, allFormatArgs, 1, formatArgs.Length);

        return questionConstructor(attr, string.Format(attr.QuestionText, allFormatArgs), correctIndex, answers.ToArray());
    }

    internal string[] GetAnswers(Question question)
    {
        SouvenirQuestionAttribute attr;
        if (!_attributes.TryGetValue(question, out attr))
            throw new InvalidOperationException(string.Format("<Souvenir #{0}> Question {1} is missing from the _attributes dictionary.", _moduleId, question));
        return attr.AllAnswers;
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
    #endregion

    #region Module handlers
    /* Generalized handlers for modules that are extremely similar */

    // Used by Affine Cycle, Caesar Cycle, Pigpen Cycle and Playfair Cycle
    private IEnumerable<object> processSpeakingEvilCycle1(KMBombModule module, string componentName, Question question, string moduleId)
    {
        var comp = GetComponent(module, componentName);
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(moduleId);

        var messages = GetArrayField<string>(comp, "message").Get();
        var responses = GetArrayField<string>(comp, "response").Get();
        var index = GetIntField(comp, "r").Get(ix =>
            ix < 0 ? "negative" :
            ix >= messages.Length ? string.Format("greater than ‘message’ length ({0})", messages.Length) :
            ix >= responses.Length ? string.Format("greater than ‘response’ length ({0})", responses.Length) : null);

        addQuestions(module,
          makeQuestion(question, moduleId, new[] { "message" }, new[] { Regex.Replace(messages[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }),
          makeQuestion(question, moduleId, new[] { "response" }, new[] { Regex.Replace(responses[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }));
    }

    // Used by Cryptic Cycle, Hill Cycle, Jumble Cycle and Ultimate Cycle
    private IEnumerable<object> processSpeakingEvilCycle2(KMBombModule module, string componentName, Question question, string moduleId)
    {
        var comp = GetComponent(module, componentName);
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(moduleId);

        var words = GetArrayField<string[]>(comp, "message").Get(expectedLength: 2);
        var messages = words[0];
        var responses = words[1];
        var index = GetIntField(comp, "r").Get(ix =>
            ix < 0 ? "‘r’ is negative." :
            ix >= messages.Length ? string.Format("‘r’ is greater than ‘message’ length ({0}).", messages.Length) :
            ix >= responses.Length ? string.Format("‘r’ is greater than ‘response’ length ({0}).", responses.Length) : null);

        addQuestions(module,
          makeQuestion(question, moduleId, new[] { "message" }, new[] { Regex.Replace(messages[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }),
          makeQuestion(question, moduleId, new[] { "response" }, new[] { Regex.Replace(responses[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }));
    }

    // Used by the World Mazes modules (currently: USA Maze, DACH Maze)
    private IEnumerable<object> processWorldMaze(KMBombModule module, string script, string moduleCode, Question question)
    {
        var comp = GetComponent(module, script);
        var fldOrigin = GetField<string>(comp, "_originState");
        var fldActive = GetField<bool>(comp, "_isActive");
        var mthGetStates = GetMethod<List<string>>(comp, "GetAllStates", 0);
        var mthGetName = GetMethod<string>(comp, "GetStateFullName", 1);

        // wait for activation
        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        // then wait for the module to get solved
        while (fldActive.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(moduleCode);

        var stateCodes = mthGetStates.Invoke();
        if (stateCodes == null)
            throw new AbandonModuleException("GetAllStates() returned null.");
        if (stateCodes.Count == 0)
            throw new AbandonModuleException("GetAllStates() returned an empty list.");

        var states = stateCodes.Select(code => mthGetName.Invoke(code)).ToArray();
        var origin = mthGetName.Invoke(fldOrigin.Get());
        if (!states.Contains(origin))
            throw new AbandonModuleException("‘_originState’ was not contained in the list of all states ({0} not in: {1}).", origin, states.JoinString(", "));

        addQuestions(module, makeQuestion(question, moduleCode, correctAnswers: new[] { origin }, preferredWrongAnswers: states));
    }

    // Used by The Hypercube and The Ultracube
    private IEnumerable<object> processHypercubeUltracube(KMBombModule module, string componentName, Question question, string moduleId)
    {
        var comp = GetComponent(module, componentName);
        var rotations = GetStaticField<string[]>(comp.GetType(), "_rotationNames").Get();
        var sequence = GetArrayField<int>(comp, "_rotations").Get(expectedLength: 5, validator: rot => rot < 0 || rot >= rotations.Length ? string.Format("expected range 0–{0}", rotations.Length - 1) : null);

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(moduleId);
        addQuestions(module,
            makeQuestion(question, moduleId, new[] { "first" }, new[] { rotations[sequence[0]] }),
            makeQuestion(question, moduleId, new[] { "second" }, new[] { rotations[sequence[1]] }),
            makeQuestion(question, moduleId, new[] { "third" }, new[] { rotations[sequence[2]] }),
            makeQuestion(question, moduleId, new[] { "fourth" }, new[] { rotations[sequence[3]] }),
            makeQuestion(question, moduleId, new[] { "fifth" }, new[] { rotations[sequence[4]] }));
    }


    /* Actual module processors start here */

    private IEnumerable<object> Process3DMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "ThreeDMazeModule");
        var fldIsComplete = GetField<bool>(comp, "isComplete");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var map = GetField<object>(comp, "map").Get();
        var mapData = GetField<Array>(map, "mapData").Get(arr => arr.GetLength(0) != 8 || arr.GetLength(1) != 8 ? string.Format("size {0},{1}, expected 8,8", arr.GetLength(0), arr.GetLength(1)) : null);
        var bearing = GetIntField(map, "end_dir").Get(min: 0, max: 3);
        var fldLabel = GetField<char>(mapData.GetValue(0, 0), "label", isPublic: true);
        var chars = new HashSet<char>();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                var ch = fldLabel.GetFrom(mapData.GetValue(i, j));
                if ("ABCDH".Contains(ch))
                    chars.Add(ch);
            }
        var correctMarkings = chars.OrderBy(c => c).JoinString();

        while (!fldIsComplete.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_3DMaze);
        addQuestions(module,
            makeQuestion(Question._3DMazeMarkings, _3DMaze, correctAnswers: new[] { correctMarkings }),
            makeQuestion(Question._3DMazeBearing, _3DMaze, correctAnswers: new[] { new[] { "North", "East", "South", "West" }[bearing] }));
    }

    private IEnumerable<object> Process3DTunnels(KMBombModule module)
    {
        var comp = GetComponent(module, "ThreeDTunnels");
        var fldSolved = GetField<bool>(comp, "_solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_3DTunnels);

        var symbols = GetStaticField<string>(comp.GetType(), "_symbols").Get();
        var targetNodeNames = GetListField<int>(comp, "_targetNodes")
            .Get(tns => tns.Any(tn => tn < 0 || tn >= symbols.Length) ? "invalid symbols" : null)
            .Select(tn => symbols[tn].ToString())
            .ToArray();
        addQuestions(module, targetNodeNames.Select((tn, ix) => makeQuestion(Question._3DTunnelsTargetNode, _3DTunnels, new[] { ordinal(ix + 1) }, new[] { tn }, targetNodeNames)));
    }

    private IEnumerable<object> ProcessAccumulation(KMBombModule module)
    {
        var comp = GetComponent(module, "accumulationScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Accumulation);

        var colorNames = new Dictionary<int, string> {
            { 9, "Blue" },
            { 23, "Brown" },
            { 4, "Green" },
            { 15, "Grey" },
            { 26, "Lime" },
            { 2, "Orange" },
            { 8, "Pink" },
            { 17, "Red" },
            { 11, "White" },
            { 10, "Yellow" }
        };

        var borderIx = GetIntField(comp, "borderValue").Get(v => !colorNames.ContainsKey(v) ? "value is not in the dictionary" : null);
        var bgNames = GetArrayField<Material>(comp, "chosenBackgroundColours", isPublic: true)
            .Get(expectedLength: 5)
            .Select(x => char.ToUpperInvariant(x.name[0]) + x.name.Substring(1))
            .ToArray();

        addQuestions(module,
            makeQuestion(Question.AccumulationBorderColor, _Accumulation, correctAnswers: new[] { colorNames[borderIx] }),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "first" }, new[] { bgNames[0] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "second" }, new[] { bgNames[1] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "third" }, new[] { bgNames[2] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "fourth" }, new[] { bgNames[3] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "fifth" }, new[] { bgNames[4] }, bgNames));
    }

    private IEnumerable<object> ProcessAdventureGame(KMBombModule module)
    {
        var comp = GetComponent(module, "AdventureGameModule");
        var fldInvWeaponCount = GetIntField(comp, "InvWeaponCount");
        var fldSelectedItem = GetIntField(comp, "SelectedItem");
        var mthItemName = GetMethod<string>(comp, "ItemName", 1);
        var mthShouldUseItem = GetMethod<bool>(comp, "ShouldUseItem", 1);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var invValues = GetField<IList>(comp, "InvValues").Get();   // actually List<AdventureGameModule.ITEM>
        var buttonUse = GetField<KMSelectable>(comp, "ButtonUse", isPublic: true).Get(b => b.OnInteract == null ? "ButtonUse.OnInteract is null" : null);
        var enemy = GetField<object>(comp, "SelectedEnemy").Get();
        var textEnemy = GetField<TextMesh>(comp, "TextEnemy", isPublic: true).Get();
        var invWeaponCount = fldInvWeaponCount.Get(v => v == 0 ? "zero" : null);

        var prevInteract = buttonUse.OnInteract;
        var origInvValues = new List<int>(invValues.Cast<int>());
        var correctItemsUsed = 0;
        var qs = new List<Func<QandA>>();
        var solved = false;

        buttonUse.OnInteract = delegate
        {
            var selectedItem = fldSelectedItem.Get();
            var itemUsed = origInvValues[selectedItem];
            var shouldUse = mthShouldUseItem.Invoke(selectedItem);
            for (int j = invWeaponCount; j < invValues.Count; j++)
                shouldUse &= !mthShouldUseItem.Invoke(j);

            var ret = prevInteract();

            if (invValues.Count != origInvValues.Count)
            {
                // If the length of the inventory has changed, the user used a correct non-weapon item.
                var itemIndex = ++correctItemsUsed;
                qs.Add(() => makeQuestion(Question.AdventureGameCorrectItem, _AdventureGame, new[] { ordinal(itemIndex) }, new[] { titleCase(mthItemName.Invoke(itemUsed)) }));
                origInvValues.Clear();
                origInvValues.AddRange(invValues.Cast<int>());
            }
            else if (shouldUse)
            {
                // The user solved the module.
                solved = true;
                textEnemy.text = "Victory!";
            }

            return ret;
        };

        while (!solved)
            yield return new WaitForSeconds(.1f);

        buttonUse.OnInteract = prevInteract;
        _modulesSolved.IncSafe(_AdventureGame);
        var enemyName = enemy.ToString();
        enemyName = enemyName.Substring(0, 1).ToUpperInvariant() + enemyName.Substring(1).ToLowerInvariant();
        addQuestions(module, qs.Select(q => q()).Concat(new[] { makeQuestion(Question.AdventureGameEnemy, _AdventureGame, correctAnswers: new[] { enemyName }) }));
    }

    private IEnumerable<object> ProcessAffineCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "AffineCycleScript", Question.AffineCycleWord, _AffineCycle);
    }

    private IEnumerable<object> ProcessAlgebra(KMBombModule module)
    {
        var comp = GetComponent(module, "algebraScript");
        var fldStage = GetIntField(comp, "stage");

        while (fldStage.Get() <= 3)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Algebra);

        addQuestions(module, Enumerable.Range(0, 2).Select(i => makeQuestion(
            question: i == 0 ? Question.AlgebraEquation1 : Question.AlgebraEquation2,
            moduleKey: _Algebra,
            correctAnswers: new[] { GetField<Texture>(comp, string.Format("level{0}Equation", i + 1)).Get().name.Replace(';', '/') })));
    }

    private IEnumerable<object> ProcessAlphabeticalRuling(KMBombModule module)
    {
        var comp = GetComponent(module, "AlphabeticalRuling");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldStage = GetIntField(comp, "currentStage");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var letterDisplay = GetField<TextMesh>(comp, "LetterDisplay", isPublic: true).Get();
        var numberDisplays = GetArrayField<TextMesh>(comp, "NumberDisplays", isPublic: true).Get(expectedLength: 2);
        var curStage = 0;
        var letters = new char[3];
        var numbers = new int[3];
        while (!fldSolved.Get())
        {
            var newStage = fldStage.Get();
            if (newStage != curStage)
            {
                if (letterDisplay.text.Length != 1 || letterDisplay.text[0] < 'A' || letterDisplay.text[0] > 'Z')
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Alphabetical Ruling because ‘LetterDisplay’ shows {1} (expected single letter A–Z).", _moduleId, letterDisplay.text);
                    yield break;
                }
                letters[newStage - 1] = letterDisplay.text[0];
                int number;
                if (!int.TryParse(numberDisplays[0].text, out number) || number < 1 || number > 9)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Alphabetical Ruling because ‘NumberDisplay[0]’ shows {1} (expected integer 1–9).", _moduleId, numberDisplays[0].text);
                    yield break;
                }
                numbers[newStage - 1] = number;
                curStage = newStage;
            }

            yield return null;
        }
        _modulesSolved.IncSafe(_AlphabeticalRuling);

        if (letters.Any(l => l < 'A' || l > 'Z') || numbers.Any(n => n < 1 || n > 9))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Alphabetical Ruling because the captured letters/numbers are unexpected (letters: [{1}], numbers: [{2}]).", _moduleId, letters.JoinString(", "), numbers.JoinString(", "));
            yield break;
        }

        var qs = new List<QandA>();
        for (var ix = 0; ix < letters.Length; ix++)
            qs.Add(makeQuestion(Question.AlphabeticalRulingLetter, _AlphabeticalRuling, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { letters[ix].ToString() }));
        for (var ix = 0; ix < numbers.Length; ix++)
            qs.Add(makeQuestion(Question.AlphabeticalRulingNumber, _AlphabeticalRuling, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { numbers[ix].ToString() }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessAlphaBits(KMBombModule module)
    {
        var comp = GetComponent(module, "AlphaBitsScript");

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };

        var displayedCharacters = new[] { "displayTL", "displayML", "displayBL", "displayTR", "displayMR", "displayBR" }.Select(fieldName => GetField<TextMesh>(comp, fieldName, isPublic: true).Get().text.Trim()).ToArray();
        if (displayedCharacters.Any(ch => ch.Length != 1 || ((ch[0] < 'A' || ch[0] > 'V') && (ch[0] < '0' || ch[0] > '9'))))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Alpha-Bits because the displayed characters are {1} (expected six single-character strings 0–9/A–V each).",
                _moduleId, displayedCharacters.Select(str => string.Format(@"""{0}""", str)).JoinString(", "));
            yield break;
        }

        while (!isSolved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_AlphaBits);

        // If the correct answer is '0' or 'O', don't include these as wrong answers.
        addQuestions(module, Enumerable.Range(0, 6).Select(displayIx => makeQuestion(
            Question.AlphaBitsDisplayedCharacters,
            _AlphaBits,
            formatArgs: new[] { new[] { "top", "middle", "bottom" }[displayIx % 3], new[] { "left", "right" }[displayIx / 3] },
            correctAnswers: new[] { displayedCharacters[displayIx] },
            preferredWrongAnswers: new AnswerGenerator.Strings(displayedCharacters[displayIx] == "0" || displayedCharacters[displayIx] == "O" ? "1-9A-NP-V" : "0-9A-V")
                .GetAnswers(this).Distinct().Take(6).ToArray())));
    }

    private IEnumerable<object> ProcessArithmelogic(KMBombModule module)
    {
        var comp = GetComponent(module, "Arithmelogic");
        var fldSymbolNum = GetIntField(comp, "submitSymbol");
        var fldSelectableValues = GetArrayField<int[]>(comp, "selectableValues");
        var fldCurrentDisplays = GetArrayField<int>(comp, "currentDisplays");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Arithmelogic);

        var symbolNum = fldSymbolNum.Get(min: 0, max: 21);
        var selVal = fldSelectableValues.Get(expectedLength: 3, validator: arr => arr.Length != 4 ? string.Format("length {0}, expected 4", arr.Length) : null);
        var curDisp = fldCurrentDisplays.Get(expectedLength: 3, validator: val => val < 0 || val >= 4 ? string.Format("expected 0–3") : null);

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.ArithmelogicSubmit, _Arithmelogic, correctAnswers: new[] { ArithmelogicSprites[symbolNum] }, preferredWrongAnswers: ArithmelogicSprites));
        var screens = new[] { "left", "middle", "right" };
        for (int i = 0; i < 3; i++)
            qs.Add(makeQuestion(Question.ArithmelogicNumbers, _Arithmelogic, formatArgs: new[] { screens[i] },
                correctAnswers: Enumerable.Range(0, 4).Where(ix => ix != curDisp[i]).Select(ix => selVal[i][ix].ToString()).ToArray()));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessBamboozledAgain(KMBombModule module)
    {
        var comp = GetComponent(module, "BamboozledAgainScript");
        var fldDisplayTexts = GetArrayField<string[]>(comp, "message");
        var fldColorIndex = GetArrayField<int>(comp, "textRandomiser");
        var fldStage = GetIntField(comp, "pressCount");
        var fldCorrectButtons = GetArrayField<int[]>(comp, "answerKey");
        var fldButtonInfo = GetArrayField<string[]>(comp, "buttonRandomiser");
        var fldButtonTextMesh = GetArrayField<TextMesh>(comp, "buttonText", isPublic: true);

        //Beginning of correct button section.

        int stage = 0;

        string[] correctButtonTexts = new string[4];
        string[] correctButtonColors = new string[4];

        //The module cycle the stage count back to 0 regardless. So it gives no indications whether the module is solved or not on the fourth press.
        //Stores the first button in a separate variable. Then, restore it once the module is solved. Index 0 for text. Index 1 for color.

        string[] correctFirstStageButton = new string[2];

        bool dataAdded = false;

        //Not certain why, but the variable 'moduleSolved' in Bamboozled Again script becomes true at the start of the submit couroutine even though the answer may not be correct.
        //Hooking isSolved variable to mitigate the possible side effects.

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };

        while (!isSolved)
        {
            var newStage = fldStage.Get(min: 0, max: 3);
            if (!dataAdded)
            {
                var buttonInfo = fldButtonInfo.Get(expectedLength: 2, validator: v => v.Length != 6 ? "expected length 6" : null);
                var correctButtons = fldCorrectButtons.Get(expectedLength: 2, validator: v => v.Length != 4 ? "expected length 4" : null);
                if (stage == 0)
                {
                    correctFirstStageButton[0] = correctButtonTexts[stage];
                    correctFirstStageButton[1] = correctButtonColors[stage];
                }
                correctButtonTexts[stage] = Regex.Replace(buttonInfo[1][correctButtons[0][stage]], "#", " ");
                correctButtonColors[stage] = buttonInfo[0][correctButtons[0][stage]][0] + buttonInfo[0][correctButtons[0][stage]].Substring(1).ToLowerInvariant();
                dataAdded = true;
            }
            if (stage != newStage)
            {
                stage = newStage;
                dataAdded = false;
            }
            var buttonTextMesh = fldButtonTextMesh.Get();

            if (buttonTextMesh == null)
                yield break;

            //Check if the module is resetting. There is no flag indicating the module is resetting, but each button will have exactly a string with length of 1 on it.
            if (buttonTextMesh.Any(strMesh => strMesh.text.Length == 1))
                dataAdded = false;

            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_BamboozledAgain);

        //Restore the first button to the arrays.

        correctButtonTexts[0] = correctFirstStageButton[0];
        correctButtonColors[0] = correctFirstStageButton[1];

        //End of correct button section.

        //Beginning of the displayed texts section.

        var displayTexts = fldDisplayTexts.Get(expectedLength: 4, validator: v => v.Length != 8 ? "expected length 8" : null).ToArray();
        var colorIndex = fldColorIndex.Get(expectedLength: 8);

        if (displayTexts[0].Any(str => string.IsNullOrEmpty(str)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Bamboozled Again because 'displayText[0]' contains null or an empty string: [{1}]", _moduleId, displayTexts[0].Select(str => str ?? "<null>").JoinString(", "));
            yield break;
        }

        displayTexts[0] = displayTexts[0].Select(str => Regex.Replace(str, "#", " ")).ToArray();

        string[] firstRowTexts = displayTexts[0].Where((item, index) => index == 0 || index == 2 || index == 4).ToArray();
        string[] lastThreeTexts = displayTexts[0].Where((item, index) => index > 4 && index < 8).ToArray();
        string[] color = new string[14] { "White", "Red", "Orange", "Yellow", "Lime", "Green", "Jade", "Grey", "Cyan", "Azure", "Blue", "Violet", "Magenta", "Rose" };
        string[] displayColors = colorIndex.Select(index => color[index]).ToArray();

        //End of the displayed texts section.

        addQuestions(module,
            correctButtonTexts.Select((name, index) => makeQuestion(Question.BamboozledAgainButtonText, _BamboozledAgain,
                formatArgs: new[] { index == 3 ? "fourth" : ordinal(index + 1) },
                correctAnswers: new[] { name },
                preferredWrongAnswers: correctButtonTexts.Except(new[] { name }).ToArray())).Concat(
            correctButtonColors.Select((col, index) => makeQuestion(Question.BamboozledAgainButtonColor, _BamboozledAgain,
                formatArgs: new[] { index == 3 ? "fourth" : ordinal(index + 1) },
                correctAnswers: new[] { col },
                preferredWrongAnswers: correctButtonColors.Except(new[] { col }).ToArray()))).Concat(
            firstRowTexts.Select((text, index) => makeQuestion(Question.BamboozledAgainDisplayTexts1, _BamboozledAgain,
                formatArgs: new[] { ordinal(2 * index + 1) },
                correctAnswers: new[] { text },
                preferredWrongAnswers: firstRowTexts.Except(new[] { text }).ToArray()))).Concat(
            lastThreeTexts.Select((text, index) => makeQuestion(Question.BamboozledAgainDisplayTexts2, _BamboozledAgain,
                formatArgs: new[] { ordinal(index + 6) },
                correctAnswers: new[] { text },
                preferredWrongAnswers: lastThreeTexts.Except(new[] { text }).ToArray()))).Concat(
            displayColors.Select((col, index) => makeQuestion(Question.BamboozledAgainDisplayColor, _BamboozledAgain,
                formatArgs: new[] { ordinal(index + 1) },
                correctAnswers: new[] { col },
                preferredWrongAnswers: displayColors.Except(new[] { col }).ToArray()))));
    }

    private IEnumerable<object> ProcessBamboozlingButton(KMBombModule module)
    {
        var comp = GetComponent(module, "BamboozlingButtonScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldRandomiser = GetArrayField<int>(comp, "randomiser");
        var fldStage = GetIntField(comp, "stage");

        var moduleData = new int[2][];
        var stage = 0;

        while (!fldSolved.Get())
        {
            var randomiser = fldRandomiser.Get(expectedLength: 11);
            var newStage = fldStage.Get(min: 1, max: 2);
            if (stage != newStage || !randomiser.SequenceEqual(moduleData[newStage - 1]))
            {
                stage = newStage;
                moduleData[stage - 1] = randomiser.ToArray(); // Take a copy of the array.
            }
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_BamboozlingButton);

        var colors = new string[15] { "White", "Red", "Orange", "Yellow", "Lime", "Green", "Jade", "Grey", "Cyan", "Azure", "Blue", "Violet", "Magenta", "Rose", "Black" };
        var texts = new string[55] { "A LETTER", "A WORD", "THE LETTER", "THE WORD", "1 LETTER", "1 WORD", "ONE LETTER", "ONE WORD", "B", "C", "D", "E", "G", "K", "N", "P", "Q", "T", "V", "W", "Y", "BRAVO", "CHARLIE", "DELTA", "ECHO", "GOLF", "KILO", "NOVEMBER", "PAPA", "QUEBEC", "TANGO", "VICTOR", "WHISKEY", "YANKEE", "COLOUR", "RED", "ORANGE", "YELLOW", "LIME", "GREEN", "JADE", "CYAN", "AZURE", "BLUE", "VIOLET", "MAGENTA", "ROSE", "IN RED", "IN YELLOW", "IN GREEN", "IN CYAN", "IN BLUE", "IN MAGENTA", "QUOTE", "END QUOTE" };
        var qs = new List<QandA>();
        for (var i = 0; i < 2; i++)
        {
            qs.Add(makeQuestion(Question.BamboozlingButtonColor, _BamboozlingButton, new[] { (i + 1).ToString() }, new[] { colors[moduleData[i][0]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplayColor, _BamboozlingButton, new[] { (i + 1).ToString(), "fourth" }, new[] { colors[moduleData[i][1]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplayColor, _BamboozlingButton, new[] { (i + 1).ToString(), "fifth" }, new[] { colors[moduleData[i][2]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "first" }, new[] { texts[moduleData[i][3]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "third" }, new[] { texts[moduleData[i][4]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "fourth" }, new[] { texts[moduleData[i][5]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "fifth" }, new[] { texts[moduleData[i][6]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonLabel, _BamboozlingButton, new[] { (i + 1).ToString(), "top" }, new[] { texts[moduleData[i][7]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonLabel, _BamboozlingButton, new[] { (i + 1).ToString(), "bottom" }, new[] { texts[moduleData[i][8]] }));
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessBartending(KMBombModule module)
    {
        var comp = GetComponent(module, "Maker");
        var fldSolved = GetField<bool>(comp, "_IsSolved");
        var fldIngredientIxs = GetArrayField<int>(comp, "ingIndices");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Bartending);

        var ingIxs = fldIngredientIxs.Get(expectedLength: 5, validator: ing => ing < 0 || ing > 4 ? "expected 0–4" : null);
        var ingredientNames = new[] { "Powdered Delta", "Flanergide", "Adelhyde", "Bronson Extract", "Karmotrine" };
        addQuestions(module, ingIxs.Select((ingIx, pos) => makeQuestion(Question.BartendingIngredients, _Bartending, formatArgs: new[] { (pos + 1).ToString() }, correctAnswers: new[] { ingredientNames[ingIx] })));
    }

    private IEnumerable<object> ProcessBigCircle(KMBombModule module)
    {
        var comp = GetComponent(module, "TheBigCircle");
        var fldSolved = GetField<bool>(comp, "_solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BigCircle);

        addQuestions(module, GetField<Array>(comp, "_currentSolution").Get(v => v.Length != 3 ? "expected length 3" : null).Cast<object>()
            .Select((color, ix) => makeQuestion(Question.BigCircleColors, _BigCircle, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { color.ToString() })));
    }

    private IEnumerable<object> ProcessBinaryLEDs(KMBombModule module)
    {
        var comp = GetComponent(module, "BinaryLeds");
        var fldSequences = GetField<int[,]>(comp, "sequences");
        var fldSequenceIndex = GetIntField(comp, "sequenceIndex");
        var fldColors = GetArrayField<int>(comp, "colorIndices");
        var fldSolutions = GetField<int[,]>(comp, "solutions");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldBlinkDelay = GetField<float>(comp, "blinkDelay");
        var mthGetIndexFromTime = GetMethod<int>(comp, "GetIndexFromTime", 2);

        int answer = -1;
        var wires = GetArrayField<KMSelectable>(comp, "wires", isPublic: true).Get(expectedLength: 3);

        for (int i = 0; i < wires.Length; i++)
        {
            // Need an extra scope to work around bug in Mono 2.0 C# compiler
            new Action<int, KMSelectable.OnInteractHandler>((j, oldInteract) =>
            {
                wires[j].OnInteract = delegate
                {
                    wires[j].OnInteract = oldInteract;  // Restore original interaction, so that this can only ever be called once per wire.
                    var wasSolved = fldSolved.Get();    // Get this before calling oldInteract()
                    var seqIx = fldSequenceIndex.Get();
                    var numIx = mthGetIndexFromTime.Invoke(Time.time, fldBlinkDelay.Get());
                    var colors = fldColors.Get(nullAllowed: true);  // We cannot risk throwing an exception during the module’s button handler
                    var solutions = fldSolutions.Get(nullAllowed: true);
                    var result = oldInteract();

                    if (wasSolved)
                        return result;

                    if (colors == null || colors.Length <= j)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning Binary LEDs because ‘colors’ array has unexpected length ({1}).", _moduleId,
                            colors == null ? "null" : colors.Length.ToString());
                        return result;
                    }

                    if (solutions == null || solutions.GetLength(0) <= seqIx || solutions.GetLength(1) <= colors[j])
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning Binary LEDs because ‘solutions’ array has unexpected lengths ({1}, {2}).", _moduleId,
                            solutions == null ? "null" : solutions.GetLength(0).ToString(),
                            solutions == null ? "null" : solutions.GetLength(1).ToString());
                        return result;
                    }

                    // Ignore if this wasn’t a solve
                    if (solutions[seqIx, colors[j]] != numIx)
                        return result;

                    // Find out which value is displayed
                    var sequences = fldSequences.Get(nullAllowed: true);

                    if (sequences == null || sequences.GetLength(0) <= seqIx || sequences.GetLength(1) <= numIx)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning Binary LEDs because ‘sequences’ array has unexpected lengths ({1}, {2}).", _moduleId,
                            sequences == null ? "null" : sequences.GetLength(0).ToString(),
                            sequences == null ? "null" : sequences.GetLength(1).ToString());
                        return result;
                    }

                    answer = sequences[seqIx, numIx];
                    return result;
                };
            })(i, wires[i].OnInteract);
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BinaryLEDs);

        if (answer != -1)
            addQuestion(module, Question.BinaryLEDsValue, correctAnswers: new[] { answer.ToString() });
    }

    private IEnumerable<object> ProcessBitmaps(KMBombModule module)
    {
        var comp = GetComponent(module, "BitmapsModule");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Bitmaps);

        var bitmap = GetArrayField<bool[]>(comp, "_bitmap").Get(expectedLength: 8, validator: arr => arr.Length != 8 ? "expected length 8" : null);
        var qCounts = new int[4];
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if (bitmap[x][y])
                    qCounts[(y / 4) * 2 + (x / 4)]++;

        var preferredWrongAnswers = qCounts.SelectMany(i => new[] { i, 16 - i }).Distinct().Select(i => i.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "top left" }, new[] { qCounts[0].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "top right" }, new[] { qCounts[1].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "bottom left" }, new[] { qCounts[2].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "bottom right" }, new[] { qCounts[3].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "top left" }, new[] { (16 - qCounts[0]).ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "top right" }, new[] { (16 - qCounts[1]).ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "bottom left" }, new[] { (16 - qCounts[2]).ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "bottom right" }, new[] { (16 - qCounts[3]).ToString() }, preferredWrongAnswers));
    }

    private IEnumerable<object> ProcessBlindMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "BlindMaze");
        var fldSolved = GetField<bool>(comp, "Solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BlindMaze);

        // Despite the name “currentMaze”, this field actually contains the number of solved modules when Blind Maze was solved
        var numSolved = GetIntField(comp, "currentMaze").Get(v => v < 0 ? "negative" : null);
        var buttonColors = GetArrayField<int>(comp, "buttonColors").Get(expectedLength: 4, validator: bc => bc < 0 || bc > 4 ? "expected 0–4" : null);

        var colorNames = new[] { "Red", "Green", "Blue", "Gray", "Yellow" };
        var buttonNames = new[] { "north", "east", "south", "west" };

        addQuestions(module,
            buttonColors.Select((col, ix) => makeQuestion(Question.BlindMazeColors, _BlindMaze, formatArgs: new[] { buttonNames[ix] }, correctAnswers: new[] { colorNames[col] }))
                .Concat(new[] { makeQuestion(Question.BlindMazeMaze, _BlindMaze, correctAnswers: new[] { ((numSolved + Bomb.GetSerialNumberNumbers().Last()) % 10).ToString() }) }));
    }

    private IEnumerable<object> ProcessBlockbusters(KMBombModule module)
    {
        var comp = GetComponent(module, "blockbustersScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var legalLetters = GetListField<string>(comp, "legalLetters", isPublic: true).Get();
        var tiles = GetField<Array>(comp, "tiles", isPublic: true).Get(arr => arr.Cast<object>().Any(v => v == null) ? "contains null" : null);
        var selectables = new KMSelectable[tiles.Length];
        var prevInteracts = new KMSelectable.OnInteractHandler[tiles.Length];
        string lastPress = null;

        for (int i = 0; i < tiles.Length; i++)
        {
            var selectable = selectables[i] = GetField<KMSelectable>(tiles.GetValue(i), "selectable", isPublic: true).Get();
            var prevInteract = prevInteracts[i] = selectable.OnInteract;
            var letter = GetField<TextMesh>(tiles.GetValue(i), "containedLetter", isPublic: true).Get();
            selectable.OnInteract = delegate
            {
                lastPress = letter.text;
                return prevInteract();
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Blockbusters);

        for (int i = 0; i < tiles.Length; i++)
            selectables[i].OnInteract = prevInteracts[i];

        if (lastPress == null)
        {
            Debug.LogFormat("[Souvenir #{0}] Abandoning Blockbusters because no pressed letter was retrieved.", _moduleId);
            yield break;
        }

        addQuestion(module, Question.BlockbustersLastLetter, correctAnswers: new[] { lastPress }, preferredWrongAnswers: legalLetters.ToArray());
    }

    private IEnumerable<object> ProcessBlueArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "BlueArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldCoord = GetField<string>(comp, "coord");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BlueArrows);

        string[] letters = { "CA", "C1", "CB", "C8", "CF", "C4", "CE", "C6", "3A", "31", "3B", "38", "3F", "34", "3E", "36", "GA", "G1", "GB", "G8", "GF", "G4", "GE", "G6", "7A", "71", "7B", "78", "7F", "74", "7E", "76", "DA", "D1", "DB", "D8", "DF", "D4", "DE", "D6", "5A", "51", "5B", "58", "5F", "54", "5E", "56", "HA", "H1", "HB", "H8", "HF", "H4", "HE", "H6", "2A", "21", "2B", "28", "2F", "24", "2E", "26" };
        string coord = fldCoord.Get(v => !letters.Contains(v) ? string.Format("expected one of: [{0}]", letters.JoinString(", ")) : null);
        addQuestion(module, Question.BlueArrowsInitialLetters, correctAnswers: new[] { coord });
    }

    private IEnumerable<object> ProcessBobBarks(KMBombModule module)
    {
        var comp = GetComponent(module, "BobBarks");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldIndicators = GetArrayField<int>(comp, "assigned");
        var fldFlashes = GetArrayField<int>(comp, "stages");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_BobBarks);

        string[] validDirections = { "top left", "top right", "bottom left", "bottom right" };
        string[] validLabels = { "BOB", "CAR", "CLR", "IND", "FRK", "FRQ", "MSA", "NSA", "SIG", "SND", "TRN", "BUB", "DOG", "ETC", "KEY" };

        int[] indicators = fldIndicators.Get(expectedLength: 4, validator: idn => idn < 0 || idn >= validLabels.Length ? string.Format("expected 0–{0}", validLabels.Length - 1) : null);
        int[] flashes = fldFlashes.Get(expectedLength: 5, validator: fn => fn < 0 || fn >= validDirections.Length ? string.Format("expected 0–{0}", validDirections.Length - 1) : null);

        // To provide preferred wrong answers, mostly.
        string[] labelsOnModule = { validLabels[indicators[0]], validLabels[indicators[1]], validLabels[indicators[2]], validLabels[indicators[3]] };

        addQuestions(module,
            Enumerable.Range(0, 4).Select(ix => makeQuestion(Question.BobBarksIndicators, _BobBarks,
                correctAnswers: new[] { labelsOnModule[ix] },
                formatArgs: new[] { validDirections[ix] },
                preferredWrongAnswers: labelsOnModule.Except(new[] { labelsOnModule[ix] }).ToArray()
            )).Concat(
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.BobBarksPositions, _BobBarks,
                correctAnswers: new[] { validDirections[flashes[ix]] },
                formatArgs: new[] { ordinal(ix + 1) }))
            ));
    }

    private IEnumerable<object> ProcessBoggle(KMBombModule module)
    {
        var comp = GetComponent(module, "boggle");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var map = GetField<char[,]>(comp, "letterMap").Get(m => m.GetLength(0) != 10 || m.GetLength(1) != 10 ? string.Format("size was {0}×{1}, expected 10×10", m.GetLength(0), m.GetLength(1)) : null);
        var visible = GetField<string>(comp, "visableLetters", isPublic: true).Get(v => v.Length != 4 ? "expected length 4" : null);
        var verOffset = GetIntField(comp, "verOffset").Get(min: 0, max: 6);
        var horOffset = GetIntField(comp, "horOffset").Get(min: 0, max: 6);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Boggle);

        var letters = new List<string>();
        for (int i = verOffset; i < verOffset + 4; i++)
            for (int j = horOffset; j < horOffset + 4; j++)
                letters.Add(map[i, j].ToString());

        addQuestion(module, Question.BoggleLetters, correctAnswers: visible.Select(v => v.ToString()).ToArray(), preferredWrongAnswers: letters.ToArray());
    }

    private IEnumerable<object> ProcessBoxing(KMBombModule module)
    {
        var comp = GetComponent(module, "boxing");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Boxing);

        var possibleNames = GetStaticField<string[]>(comp.GetType(), "possibleNames").Get();
        var possibleSubstituteNames = GetStaticField<string[]>(comp.GetType(), "possibleSubstituteNames").Get();
        var possibleLastNames = GetStaticField<string[]>(comp.GetType(), "possibleLastNames").Get();
        var contestantStrengths = GetArrayField<int>(comp, "contestantStrengths").Get(expectedLength: 5);
        var contestantIndices = GetArrayField<int>(comp, "contestantIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleNames.Length ? "out of range" : null);
        var lastNameIndices = GetArrayField<int>(comp, "lastNameIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleLastNames.Length ? "out of range" : null);
        var substituteIndices = GetArrayField<int>(comp, "substituteIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleSubstituteNames.Length ? "out of range" : null);
        var substituteLastNameIndices = GetArrayField<int>(comp, "substituteLastNameIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleLastNames.Length ? "out of range" : null);

        var qs = new List<QandA>();
        for (var ct = 0; ct < 5; ct++)
        {
            qs.Add(makeQuestion(Question.BoxingStrengthByContestant, _Boxing, formatArgs: new[] { possibleNames[contestantIndices[ct]] }, correctAnswers: new[] { contestantStrengths[ct].ToString() }));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "first name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleNames[contestantIndices[ct]] }, preferredWrongAnswers: possibleNames));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "last name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleLastNames[lastNameIndices[ct]] }, preferredWrongAnswers: possibleLastNames));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "substitute’s first name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleSubstituteNames[substituteIndices[ct]] }, preferredWrongAnswers: possibleSubstituteNames));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "substitute’s last name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleLastNames[substituteLastNameIndices[ct]] }, preferredWrongAnswers: possibleLastNames));
        }
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "contestant’s first name", }, correctAnswers: contestantIndices.Select(ix => possibleNames[ix]).ToArray(), preferredWrongAnswers: possibleNames));
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "contestant’s last name" }, correctAnswers: lastNameIndices.Select(ix => possibleLastNames[ix]).ToArray(), preferredWrongAnswers: possibleLastNames));
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "substitute’s first name" }, correctAnswers: substituteIndices.Select(ix => possibleSubstituteNames[ix]).ToArray(), preferredWrongAnswers: possibleSubstituteNames));
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "substitute’s last name" }, correctAnswers: substituteLastNameIndices.Select(ix => possibleLastNames[ix]).ToArray(), preferredWrongAnswers: possibleLastNames));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessBraille(KMBombModule module)
    {
        var comp = GetComponent(module, "BrailleModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Braille);
        addQuestion(module, Question.BrailleWord, correctAnswers: new[] { GetField<string>(comp, "_word").Get() });
    }

    private IEnumerable<object> ProcessBrokenButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "BrokenButtonModule");
        var fldSolved = GetField<bool>(comp, "Solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BrokenButtons);

        var pressed = GetListField<string>(comp, "Pressed").Get();
        if (pressed.All(p => p.Length == 0))
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Broken Buttons because the only buttons you pressed were literally blank.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        // skip the literally blank buttons.
        addQuestions(module, pressed.Select((p, i) => p.Length == 0 ? null : makeQuestion(Question.BrokenButtons, _BrokenButtons, new[] { ordinal(i + 1) }, new[] { p }, pressed.Except(new[] { "" }).ToArray())));
    }

    private IEnumerable<object> ProcessBrushStrokes(KMBombModule module)
    {
        var comp = GetComponent(module, "BrushStrokesScript");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BrushStrokes);

        string[] colorNames = GetStaticField<string[]>(comp.GetType(), "colorNames").Get();
        int[] colors = GetArrayField<int>(comp, "colors").Get(expectedLength: 9);

        if (colors[4] < 0 || colors[4] >= colorNames.Length)
            throw new AbandonModuleException("‘colors[4]’ pointed to illegal color: {0}.", colors[4]);

        addQuestion(module, Question.BrushStrokesMiddleColor, correctAnswers: new[] { char.ToUpperInvariant(colorNames[colors[4]][0]) + colorNames[colors[4]].Substring(1) });
    }

    private IEnumerable<object> ProcessBulb(KMBombModule module)
    {
        var comp = GetComponent(module, "TheBulbModule");
        var fldStage = GetIntField(comp, "_stage");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        while (fldStage.Get() != 0)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Bulb);

        addQuestion(module, Question.BulbButtonPresses, correctAnswers: new[] { GetField<string>(comp, "_correctButtonPresses").Get(str => str.Length != 3 ? "expected length 3" : null) });
    }

    private IEnumerable<object> ProcessBurglarAlarm(KMBombModule module)
    {
        var comp = GetComponent(module, "BurglarAlarmScript");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BurglarAlarm);

        var displayText = GetField<TextMesh>(comp, "DisplayText", isPublic: true).Get();
        displayText.text = "";

        var moduleNumber = GetArrayField<int>(comp, "moduleNumber").Get(expectedLength: 8, validator: mn => mn < 0 || mn > 9 ? "expected 0–9" : null);
        addQuestions(module, moduleNumber.Select((mn, ix) => makeQuestion(Question.BurglarAlarmDigits, _BurglarAlarm, new[] { ordinal(ix + 1) }, new[] { mn.ToString() }, moduleNumber.Select(n => n.ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessButton(KMBombModule module)
    {
        var comp = GetComponent(module, "ButtonComponent");
        var fldSolved = GetField<bool>(comp, "IsSolved", true);
        var propLightColor = GetProperty<object>(comp, "IndicatorColor", true);
        var ledOff = GetField<GameObject>(comp, "LED_Off", true).Get();

        var color = -1;
        while (!fldSolved.Get())
        {
            color = ledOff.activeSelf ? -1 : (int) propLightColor.Get();
            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_Button);
        if (color < 0)
        {
            Debug.LogFormat("[Souvenir #{0}] No question for The Button because the button was tapped (or I missed the light color).", _moduleId);
            _legitimatelyNoQuestions.Add(module);
        }
        else
        {
            string answer;
            switch (color)
            {
                case 0: answer = "red"; break;
                case 1: answer = "blue"; break;
                case 2: answer = "yellow"; break;
                case 3: answer = "white"; break;
                default: Debug.LogFormat("<Souvenir #{0}> Abandoning The Button because IndicatorColor is out of range ({1}).", _moduleId, color); yield break;
            }
            addQuestion(module, Question.ButtonLightColor, correctAnswers: new[] { answer });
        }
    }

    private IEnumerable<object> ProcessButtonSequences(KMBombModule module)
    {
        var comp = GetComponent(module, "ButtonSequencesModule");
        var fldButtonsActive = GetField<bool>(comp, "buttonsActive");

        while (fldButtonsActive.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ButtonSequences);

        var panelInfo = GetField<Array>(comp, "PanelInfo").Get(arr =>
            arr.Rank != 2 ? string.Format("has rank {0}, expected 2", arr.Rank) :
            arr.GetLength(1) != 3 ? string.Format("GetLength(1) == {0}, expected 3", arr.GetLength(1)) :
            Enumerable.Range(0, arr.GetLength(0)).Any(x => Enumerable.Range(0, arr.GetLength(1)).Any(y => arr.GetValue(x, y) == null)) ? "contains null" : null);

        var obj = panelInfo.GetValue(0, 0);
        var fldColor = GetIntField(obj, "color", isPublic: true);
        var colorNames = GetArrayField<string>(comp, "ColorNames").Get();
        var colorOccurrences = new Dictionary<int, int>();
        for (int i = panelInfo.GetLength(0) - 1; i >= 0; i--)
            for (int j = 0; j < 3; j++)
                colorOccurrences.IncSafe(fldColor.GetFrom(panelInfo.GetValue(i, j), v => v < 0 || v >= colorNames.Length ? string.Format("out of range; colorNames.Length={0} ([{1}])", colorNames.Length, colorNames.JoinString(", ")) : null));

        addQuestions(module, colorOccurrences.Select(kvp =>
            makeQuestion(Question.ButtonSequencesColorOccurrences, _ButtonSequences,
                formatArgs: new[] { colorNames[kvp.Key].ToLowerInvariant() },
                correctAnswers: new[] { kvp.Value.ToString() },
                preferredWrongAnswers: colorOccurrences.Values.Select(v => v.ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessCaesarCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "CaesarCycleScript", Question.CaesarCycleWord, _CaesarCycle);
    }

    private IEnumerable<object> ProcessCalendar(KMBombModule module)
    {
        var comp = GetComponent(module, "calendar");
        var fldLightsOn = GetField<bool>(comp, "_lightsOn");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");

        while (!fldLightsOn.Get())
            yield return new WaitForSeconds(.1f);

        var colorblindText = GetField<TextMesh>(comp, "colorblindText", isPublic: true).Get(v => v.text == null ? "text is null" : null);

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Calendar);

        addQuestion(module, Question.CalendarLedColor, correctAnswers: new[] { colorblindText.text });
    }

    private IEnumerable<object> ProcessChallengeAndContact(KMBombModule module)
    {
        var comp = GetComponent(module, "moduleScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldAnswers = GetArrayField<string>(comp, "answers");
        var fldFirstSet = GetArrayField<string>(comp, "possibleFirstAnswers");
        var fldSecondSet = GetArrayField<string>(comp, "possibleSecondAnswers");
        var fldThirdSet = GetArrayField<string>(comp, "possibleFinalAnswers");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ChallengeAndContact);

        string[] answers = fldAnswers.Get(expectedLength: 3);
        string[] firstSet = fldFirstSet.Get();
        string[] secondSet = fldSecondSet.Get();
        string[] thirdSet = fldThirdSet.Get();

        string[] allAnswers = new string[firstSet.Length + secondSet.Length + thirdSet.Length];
        firstSet.CopyTo(allAnswers, 0);
        secondSet.CopyTo(allAnswers, firstSet.Length);
        thirdSet.CopyTo(allAnswers, firstSet.Length + secondSet.Length);

        for (int i = 0; i < answers.Length; i++)
            answers[i] = char.ToUpperInvariant(answers[i][0]) + answers[i].Substring(1);
        for (int i = 0; i < allAnswers.Length; i++)
            allAnswers[i] = char.ToUpperInvariant(allAnswers[i][0]) + allAnswers[i].Substring(1);

        addQuestions(module,
            makeQuestion(Question.ChallengeAndContactAnswers, _ChallengeAndContact, new[] { "first" }, new[] { answers[0] }, allAnswers.Where(x => x[0] == answers[0][0]).ToArray()),
            makeQuestion(Question.ChallengeAndContactAnswers, _ChallengeAndContact, new[] { "second" }, new[] { answers[1] }, allAnswers.Where(x => x[0] == answers[1][0]).ToArray()),
            makeQuestion(Question.ChallengeAndContactAnswers, _ChallengeAndContact, new[] { "third" }, new[] { answers[2] }, allAnswers.Where(x => x[0] == answers[2][0]).ToArray()));
    }

    private IEnumerable<object> ProcessCheapCheckout(KMBombModule module)
    {
        var comp = GetComponent(module, "CheapCheckoutModule");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var paids = new List<decimal> { GetField<decimal>(comp, "Display").Get() };
        var paid = GetField<decimal>(comp, "Paid").Get();
        if (paid != paids[0])
            paids.Add(paid);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_CheapCheckout);

        addQuestions(module, paids.Select((p, i) => makeQuestion(Question.CheapCheckoutPaid, _CheapCheckout,
            formatArgs: new[] { paids.Count == 1 ? "" : ordinal(i + 1) + " " },
            correctAnswers: new[] { "$" + p.ToString("N2") })));
    }

    private IEnumerable<object> ProcessChess(KMBombModule module)
    {
        var comp = GetComponent(module, "ChessBehaviour");
        var fldIndexSelected = GetArrayField<int>(comp, "indexSelected"); // this contains both the coordinates and the solution
        var fldIsSolved = GetField<bool>(comp, "isSolved", isPublic: true);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var indexSelected = fldIndexSelected.Get(expectedLength: 7, validator: b => b / 10 < 0 || b / 10 >= 6 || b % 10 < 0 || b % 10 >= 6 ? "unexpected value" : null);

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Chess);

        addQuestions(module, Enumerable.Range(0, 6).Select(i => makeQuestion(Question.ChessCoordinate, _Chess, new[] { ordinal(i + 1) }, new[] { "" + ((char) (indexSelected[i] / 10 + 'a')) + (indexSelected[i] % 10 + 1) })));
    }

    private IEnumerable<object> ProcessChineseCounting(KMBombModule module)
    {
        var comp = GetComponent(module, "chineseCounting");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ChineseCounting);

        var index1 = GetIntField(comp, "ledIndex").Get(0, 3);
        var index2 = GetIntField(comp, "led2Index").Get(0, 3);
        var ledColors = new[] { "White", "Red", "Green", "Orange" };

        addQuestions(module,
          makeQuestion(Question.ChineseCountingLED, _ChineseCounting, new[] { "left" }, new[] { ledColors[index1] }),
          makeQuestion(Question.ChineseCountingLED, _ChineseCounting, new[] { "right" }, new[] { ledColors[index2] }));
    }

    private IEnumerable<object> ProcessChordQualities(KMBombModule module)
    {
        var comp = GetComponent(module, "ChordQualities");
        var fldIsSolved = GetField<bool>(comp, "isSolved", isPublic: true);

        var givenChord = GetField<object>(comp, "givenChord").Get();
        var quality = GetField<object>(givenChord, "quality").Get();
        var qualityName = GetField<string>(quality, "name").Get();
        var lights = GetField<Array>(comp, "lights", isPublic: true).Get(v => v.Length != 12 ? "expected length 12" : null);
        var mthsSetOutputLight = lights.Cast<object>().Select(light => GetMethod<object>(light, "setOutputLight", numParameters: 1, isPublic: true)).ToArray();

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ChordQualities);

        foreach (var method in mthsSetOutputLight)
            method.Invoke(false);

        var noteNames = GetField<Array>(givenChord, "notes").Get(v => v.Length != 4 ? "expected length 4" : null).Cast<object>().Select(note => note.ToString().Replace("sharp", "♯")).ToArray();
        addQuestions(module,
            makeQuestion(Question.ChordQualitiesNotes, _ChordQualities, correctAnswers: noteNames),
            makeQuestion(Question.ChordQualitiesQuality, _ChordQualities, correctAnswers: new[] { qualityName }));
    }

    private IEnumerable<object> ProcessCode(KMBombModule module)
    {
        var comp = GetComponent(module, "TheCodeModule");
        var fldCode = GetIntField(comp, "moduleNumber");
        var fldResetBtn = GetField<KMSelectable>(comp, "ButtonR", isPublic: true);
        var fldSubmitBtn = GetField<KMSelectable>(comp, "ButtonS", isPublic: true);

        var code = fldCode.Get(min: 999, max: 9999);

        // Hook into the module’s OnPass handler
        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        yield return new WaitUntil(() => isSolved);
        _modulesSolved.IncSafe(_Code);

        // Block the submit/reset buttons
        fldResetBtn.Get().OnInteract = delegate { return false; };
        fldSubmitBtn.Get().OnInteract = delegate { return false; };

        addQuestions(module, makeQuestion(Question.CodeDisplayNumber, _Code, correctAnswers: new[] { code.ToString() }));
    }

    private IEnumerable<object> ProcessCoffeebucks(KMBombModule module)
    {
        var comp = GetComponent(module, "coffeebucksScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Coffeebucks);

        var names = GetArrayField<string>(comp, "nameOptions", isPublic: true).Get();
        var coffees = GetArrayField<string>(comp, "coffeeOptions", isPublic: true).Get();
        var currName = GetIntField(comp, "startName").Get(min: 0, max: names.Length - 1);
        var currCoffee = GetIntField(comp, "startCoffee").Get(min: 0, max: coffees.Length - 1);

        for (int i = 0; i < names.Length; i++)
            names[i] = char.ToUpperInvariant(names[i][0]) + names[i].Substring(1);
        for (int i = 0; i < coffees.Length; i++)
            coffees[i] = coffees[i].Replace("\n", " ");

        addQuestions(module,
            makeQuestion(Question.CoffeebucksClient, _Coffeebucks, correctAnswers: new[] { names[currName] }, preferredWrongAnswers: names),
            makeQuestion(Question.CoffeebucksCoffee, _Coffeebucks, correctAnswers: new[] { coffees[currCoffee] }, preferredWrongAnswers: coffees));
    }

    private IEnumerable<object> ProcessColorBraille(KMBombModule module)
    {
        var comp = GetComponent(module, "ColorBrailleModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColorBraille);

        var manglingNames = new Dictionary<string, string>
        {
            { "TopRowShiftedToTheRight", "Top row shifted to the right" },
            { "TopRowShiftedToTheLeft", "Top row shifted to the left" },
            { "MiddleRowShiftedToTheRight", "Middle row shifted to the right" },
            { "MiddleRowShiftedToTheLeft", "Middle row shifted to the left" },
            { "BottomRowShiftedToTheRight", "Bottom row shifted to the right" },
            { "BottomRowShiftedToTheLeft", "Bottom row shifted to the left" },
            { "EachLetterUpsideDown", "Each letter upside-down" },
            { "EachLetterHorizontallyFlipped", "Each letter horizontally flipped" },
            { "EachLetterVerticallyFlipped", "Each letter vertically flipped" },
            { "DotsAreInverted", "Dots are inverted" }
        };

        var allWordsType = comp.GetType().Assembly.GetType("ColorBraille.WordsData");
        if (allWordsType == null)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Braille because I cannot find the ColorBraille.WordsData type.", _moduleId);
            yield break;
        }
        var allWords = GetStaticField<Dictionary<string, int[]>>(allWordsType, "Words", isPublic: true).Get().Keys.ToArray();

        var words = GetArrayField<string>(comp, "_words").Get(expectedLength: 3);
        var mangling = GetField<object>(comp, "_mangling").Get(m => !manglingNames.ContainsKey(m.ToString()) ? "mangling is not in the dictionary" : null);
        addQuestions(module,
            makeQuestion(Question.ColorBrailleWords, _ColorBraille, formatArgs: new[] { "red" }, correctAnswers: new[] { words[0] }, preferredWrongAnswers: allWords),
            makeQuestion(Question.ColorBrailleWords, _ColorBraille, formatArgs: new[] { "green" }, correctAnswers: new[] { words[1] }, preferredWrongAnswers: allWords),
            makeQuestion(Question.ColorBrailleWords, _ColorBraille, formatArgs: new[] { "blue" }, correctAnswers: new[] { words[2] }, preferredWrongAnswers: allWords),
            makeQuestion(Question.ColorBrailleMangling, _ColorBraille, correctAnswers: new[] { manglingNames[mangling.ToString()] }));
    }

    private IEnumerable<object> ProcessColorDecoding(KMBombModule module)
    {
        var comp = GetComponent(module, "ColorDecoding");
        var fldInputButtons = GetArrayField<KMSelectable>(comp, "InputButtons", isPublic: true);
        var fldStageNum = GetIntField(comp, "stagenum");
        var fldIndicator = GetField<object>(comp, "indicator");
        var indicatorGrid = GetArrayField<GameObject>(comp, "IndicatorGrid", isPublic: true).Get();

        var patterns = new Dictionary<int, string>();
        var colors = new Dictionary<int, string[]>();
        var isSolved = false;
        var isAbandoned = false;

        var inputButtons = fldInputButtons.Get();
        var origInteract = inputButtons.Select(ib => ib.OnInteract).ToArray();
        object lastIndicator = null;

        var colorNameMapping = new Dictionary<string, string>
        {
            { "R", "Red" },
            { "G", "Green" },
            { "B", "Blue" },
            { "Y", "Yellow" },
            { "P", "Purple" }
        };

        var update = new Action(() =>
        {
            // We mustn’t throw an exception during the module’s button handler
            try
            {
                var ind = fldIndicator.Get();
                if (ReferenceEquals(ind, lastIndicator))
                    return;
                lastIndicator = ind;
                var indColors = GetField<IList>(ind, "indicator_colors").Get(
                    v => v.Count == 0 ? "no indicator colors" :
                    v.Cast<object>().Any(col => !colorNameMapping.ContainsKey(col.ToString())) ? "color is not in the color name mapping" : null);
                var stageNum = fldStageNum.Get();
                var patternName = GetField<object>(ind, "pattern").Get().ToString();
                patterns[stageNum] = patternName.Substring(0, 1) + patternName.Substring(1).ToLowerInvariant();
                colors[stageNum] = indColors.Cast<object>().Select(obj => colorNameMapping[obj.ToString()]).ToArray();
            }
            catch (AbandonModuleException amex)
            {
                Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Decoding because: {1}", _moduleId, amex.Message);
                isAbandoned = true;
            }
        });
        update();

        for (int ix = 0; ix < inputButtons.Length; ix++)
        {
            new Action<int>(i =>
            {
                inputButtons[i].OnInteract = delegate
                {
                    var ret = origInteract[i]();
                    if (isSolved || isAbandoned)
                        return ret;

                    if (fldStageNum.Get() >= 3)
                    {
                        for (int j = 0; j < indicatorGrid.Length; j++)
                            indicatorGrid[j].GetComponent<MeshRenderer>().material.color = Color.black;
                        isSolved = true;
                    }
                    else
                        update();

                    return ret;
                };
            })(ix);
        }

        while (!isSolved && !isAbandoned)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColorDecoding);

        for (int ix = 0; ix < inputButtons.Length; ix++)
            inputButtons[ix].OnInteract = origInteract[ix];

        if (isAbandoned)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Decoding.", _moduleId);
            yield break;
        }

        if (Enumerable.Range(0, 3).Any(k => !patterns.ContainsKey(k) || !colors.ContainsKey(k)))
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Decoding because I have a discontinuous set of stages: {1}/{2}.", _moduleId, patterns.Keys.JoinString(", "), colors.Keys.JoinString(", "));
            yield break;
        }

        addQuestions(module, Enumerable.Range(0, 3).SelectMany(stage => Ut.NewArray(
             colors[stage].Length <= 3 ? makeQuestion(Question.ColorDecodingIndicatorColors, _ColorDecoding, new[] { "appeared", ordinal(stage + 1) }, colors[stage]) : null,
             colors[stage].Length >= 3 ? makeQuestion(Question.ColorDecodingIndicatorColors, _ColorDecoding, new[] { "did not appear", ordinal(stage + 1) }, colorNameMapping.Keys.Except(colors[stage]).ToArray()) : null,
             makeQuestion(Question.ColorDecodingIndicatorPattern, _ColorDecoding, new[] { ordinal(stage + 1) }, new[] { patterns[stage] }))));
    }

    private IEnumerable<object> ProcessColoredKeys(KMBombModule module)
    {
        var comp = GetComponent(module, "ColoredKeysScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColoredKeys);

        var colors = GetArrayField<string>(comp, "loggingWords", isPublic: true).Get();
        var letters = GetArrayField<string>(comp, "letters", isPublic: true).Get();
        var displayWord = GetIntField(comp, "displayIndex").Get(0, colors.Length - 1);
        var displayColor = GetIntField(comp, "displayColIndex").Get(0, colors.Length - 1);
        var matsNames = GetArrayField<Material>(comp, "buttonmats", isPublic: true).Get().Select(x => x.name).ToArray();

        var btnLetter = Enumerable.Range(1, 4).Select(i => GetIntField(comp, string.Format("b{0}LetIndex", i)).Get(0, letters.Length - 1)).ToArray();
        var btnColor = Enumerable.Range(1, 4).Select(i => GetIntField(comp, string.Format("b{0}ColIndex", i)).Get(0, matsNames.Length - 1)).ToArray();

        addQuestions(module,
            makeQuestion(Question.ColoredKeysDisplayWord, _ColoredKeys, correctAnswers: new[] { colors[displayWord] }, preferredWrongAnswers: colors),
            makeQuestion(Question.ColoredKeysDisplayWordColor, _ColoredKeys, correctAnswers: new[] { colors[displayColor] }, preferredWrongAnswers: colors),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "top-left" }, new[] { letters[btnLetter[0]] }, letters),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "top-right" }, new[] { letters[btnLetter[1]] }, letters),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "bottom-left" }, new[] { letters[btnLetter[2]] }, letters),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "bottom-right" }, new[] { letters[btnLetter[3]] }, letters),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "top-left" }, new[] { matsNames[btnColor[0]] }, matsNames),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "top-right" }, new[] { matsNames[btnColor[1]] }, matsNames),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "bottom-left" }, new[] { matsNames[btnColor[2]] }, matsNames),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "bottom-right" }, new[] { matsNames[btnColor[3]] }, matsNames));
    }

    private IEnumerable<object> ProcessColoredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "ColoredSquaresModule");
        var fldExpectedPresses = GetField<object>(comp, "_expectedPresses");

        // Colored Squares sets _expectedPresses to null when it’s solved
        while (fldExpectedPresses.Get(nullAllowed: true) != null)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_ColoredSquares);
        addQuestion(module, Question.ColoredSquaresFirstGroup, correctAnswers: new[] { GetField<object>(comp, "_firstStageColor").Get().ToString() });
    }

    private IEnumerable<object> ProcessColoredSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "ColoredSwitchesModule");
        var fldSwitches = GetIntField(comp, "_switchState");
        var fldSolution = GetIntField(comp, "_solutionState");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        var initial = fldSwitches.Get(0, (1 << 5) - 1);

        while (fldSolution.Get() == -1)
            yield return null;  // not waiting for .1 seconds this time to make absolutely sure we catch it before the player toggles another switch

        var afterReveal = fldSwitches.Get(0, (1 << 5) - 1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColoredSwitches);
        addQuestions(module,
            makeQuestion(Question.ColoredSwitchesInitialPosition, _ColoredSwitches, correctAnswers: new[] { Enumerable.Range(0, 5).Select(b => (initial & (1 << b)) != 0 ? "Q" : "R").Reverse().JoinString() }),
            makeQuestion(Question.ColoredSwitchesWhenLEDsCameOn, _ColoredSwitches, correctAnswers: new[] { Enumerable.Range(0, 5).Select(b => (afterReveal & (1 << b)) != 0 ? "Q" : "R").Reverse().JoinString() }));
    }

    private IEnumerable<object> ProcessColorMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "ColorMorseModule");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        // Once Color Morse is activated, ‘flashingEnabled’ is set to true, and then it is only set to false when the module is solved.
        var fldFlashingEnabled = GetField<bool>(comp, "flashingEnabled");
        while (fldFlashingEnabled.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColorMorse);

        var numbers = GetArrayField<int>(comp, "Numbers").Get(expectedLength: 3);
        var colorNames = GetArrayField<string>(comp, "ColorNames", isPublic: true).Get();
        var colors = GetArrayField<int>(comp, "Colors").Get(expectedLength: 3, validator: c => c < 0 || c >= colorNames.Length ? "out of range" : null);

        var flashedColorNames = colors.Select(c => colorNames[c].Substring(0, 1) + colorNames[c].Substring(1).ToLowerInvariant()).ToArray();
        var flashedCharacters = numbers.Select(num => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(num, 1)).ToArray();

        addQuestions(module, Enumerable.Range(0, 3).SelectMany(ix => Ut.NewArray(
             makeQuestion(Question.ColorMorseColor, _ColorMorse, new[] { ordinal(ix + 1) }, new[] { flashedColorNames[ix] }, flashedColorNames),
             makeQuestion(Question.ColorMorseCharacter, _ColorMorse, new[] { ordinal(ix + 1) }, new[] { flashedCharacters[ix] }, flashedCharacters))));
    }

    private IEnumerable<object> ProcessCoordinates(KMBombModule module)
    {
        var comp = GetComponent(module, "CoordinatesModule");
        var fldFirstSubmitted = GetField<int?>(comp, "_firstCorrectSubmitted");

        while (fldFirstSubmitted.Get(nullAllowed: true) == null)
            yield return new WaitForSeconds(.1f);

        var fldClues = GetField<IList>(comp, "_clues");
        var clues = fldClues.Get();
        var index = fldFirstSubmitted.Get(v => v < 0 || v >= clues.Count ? string.Format("out of range; clues.Count={0}", clues.Count) : null).Value;
        var clue = clues[index];
        var fldClueText = GetField<string>(clue, "Text");
        var fldClueSystem = GetField<int?>(clue, "System");
        var clueText = fldClueText.Get();

        // The module sets ‘clues’ to null to indicate that it is solved.
        while (fldClues.Get(nullAllowed: true) != null)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Coordinates);
        var shortenCoordinate = Ut.Lambda((string str) =>
        {
            if (str == null)
                return null;

            str = str.Replace("\n", " ");
            if (str.Length > 13)
            {
                str = str
                    .Replace(",", "")
                    .Replace("north", "N")
                    .Replace("south", "S")
                    .Replace("west", "W")
                    .Replace("east", "E")
                    .Replace("up", "U")
                    .Replace("down", "D")
                    .Replace("left", "L")
                    .Replace("right", "R")
                    .Replace("top", "T")
                    .Replace("bottom", "B")
                    .Replace("middle", "M")
                    .Replace("center", "C")
                    .Replace("from", "fr.")
                    .Replace(" o’clock", "")
                    .Replace(" corner", "");
                str = Regex.Replace(str, @"\b[A-Z] [A-Z]\b", m => m.Value.Remove(1, 1));
            }
            return str;
        });

        // The size clue is the only one where fldClueSystem is null
        var sizeClue = clues.Cast<object>().Where(szCl => fldClueSystem.GetFrom(szCl, nullAllowed: true) == null).FirstOrDefault();
        addQuestions(module,
            makeQuestion(Question.CoordinatesFirstSolution, _Coordinates, correctAnswers: new[] { shortenCoordinate(clueText) }, preferredWrongAnswers: clues.Cast<object>().Select(c => shortenCoordinate(fldClueText.GetFrom(c))).Where(t => t != null).ToArray()),
            sizeClue == null ? null : makeQuestion(Question.CoordinatesSize, _Coordinates, correctAnswers: new[] { fldClueText.GetFrom(sizeClue) }));
    }

    private IEnumerable<object> ProcessCorners(KMBombModule module)
    {
        var comp = GetComponent(module, "CornersModule");
        var fldSolved = GetField<bool>(comp, "_moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Corners);

        var colorNames = new[] { "red", "green", "blue", "yellow" };
        var cornerNames = new[] { "top-left", "top-right", "bottom-right", "bottom-left" };

        var clampColors = GetArrayField<int>(comp, "_clampColors").Get(expectedLength: 4, validator: v => v < 0 || v >= colorNames.Length ? string.Format("expected 0–{0}", colorNames.Length - 1) : null);
        var qs = new List<QandA>();
        qs.AddRange(cornerNames.Select((corner, cIx) => makeQuestion(Question.CornersColors, _Corners, formatArgs: new[] { corner }, correctAnswers: new[] { colorNames[clampColors[cIx]] })));
        qs.AddRange(colorNames.Select((col, colIx) => makeQuestion(Question.CornersColorCount, _Corners, formatArgs: new[] { col }, correctAnswers: new[] { clampColors.Count(cc => cc == colIx).ToString() })));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessCrackbox(KMBombModule module)
    {
        var comp = GetComponent(module, "CrackboxScript");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Crackbox);

        var array = GetField<Array>(comp, "originalGridItems").Get(arr => arr.Length != 16 ? "expected length 16" : null);
        var obj = array.GetValue(0);
        var fldIsBlack = GetField<bool>(obj, "IsBlack", isPublic: true);
        var fldIsLocked = GetField<bool>(obj, "IsLocked", isPublic: true);
        var fldValue = GetIntField(obj, "Value", isPublic: true);

        var qs = new List<QandA>();
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                obj = array.GetValue(y * 4 + x);
                qs.Add(makeQuestion(Question.CrackboxInitialState, _Crackbox, new[] { ((char) ('A' + x)).ToString(), (y + 1).ToString() }, new[] { fldIsBlack.GetFrom(obj) ? "black" : !fldIsLocked.GetFrom(obj) ? "white" : fldValue.GetFrom(obj).ToString() }));
            }
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessCreation(KMBombModule module)
    {
        var comp = GetComponent(module, "CreationModule");
        var fldSolved = GetField<bool>(comp, "Solved");
        var fldDay = GetIntField(comp, "Day");
        var fldWeather = GetField<string>(comp, "Weather");

        var weatherNames = GetAnswers(Question.CreationWeather);

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var currentDay = fldDay.Get(min: 1, max: 1);
        var currentWeather = fldWeather.Get(cw => !weatherNames.Contains(cw) ? "unknown weather" : null);
        var allWeather = new List<string>();
        while (true)
        {
            while (fldDay.Get() == currentDay && !fldSolved.Get() && currentWeather == fldWeather.Get())
                yield return new WaitForSeconds(0.1f);

            if (fldSolved.Get())
                break;

            if (fldDay.Get() <= currentDay)
                allWeather.Clear();
            else
                allWeather.Add(currentWeather);

            currentDay = fldDay.Get(min: 1, max: 6);
            currentWeather = fldWeather.Get(cw => !weatherNames.Contains(cw) ? "unknown weather" : null);
        }

        _modulesSolved.IncSafe(_Creation);
        addQuestions(module, allWeather.Select((t, i) => makeQuestion(Question.CreationWeather, _Creation, new[] { ordinal(i + 1) }, new[] { t })));
    }

    private IEnumerable<object> ProcessCrypticCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "CrypticCycleScript", Question.CrypticCycleWord, _CrypticCycle);
    }

    private IEnumerable<object> ProcessCube(KMBombModule module)
    {
        var comp = GetComponent(module, "theCubeScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Cube);

        var rotations = GetListField<int>(comp, "selectedRotations").Get(expectedLength: 6);
        var rotationNames = new[] { "rotate cw", "tip left", "tip backwards", "rotate ccw", "tip right", "tip forwards" };
        var allRotations = rotations.Select(r => rotationNames[r]).ToArray();

        addQuestions(module, rotations.Select((rot, ix) => makeQuestion(Question.CubeRotations, _Cube, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { rotationNames[rot] }, preferredWrongAnswers: allRotations)));
    }

    private IEnumerable<object> ProcessDACHMaze(KMBombModule module)
    {
        return processWorldMaze(module, "DACHMaze", _DACHMaze, Question.DACHMazeOrigin);
    }

    private IEnumerable<object> ProcessDeckOfManyThings(KMBombModule module)
    {
        var comp = GetComponent(module, "deckOfManyThingsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldSolution = GetIntField(comp, "solution");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DeckOfManyThings);

        var deck = GetField<Array>(comp, "deck").Get(d => d.Length == 0 ? "deck is empty" : null);
        var btns = GetArrayField<KMSelectable>(comp, "btns", isPublic: true).Get(expectedLength: 2);
        var prevCard = GetField<KMSelectable>(comp, "prevCard", isPublic: true).Get();
        var nextCard = GetField<KMSelectable>(comp, "nextCard", isPublic: true).Get();

        prevCard.OnInteract = delegate { return false; };
        nextCard.OnInteract = delegate { return false; };
        foreach (var btn in btns)
            btn.OnInteract = delegate
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
                btn.AddInteractionPunch(0.5f);
                return false;
            };

        string firstCardDeck = deck.GetValue(0).GetType().ToString().Replace("Card", "");

        // correcting original misspelling
        if (firstCardDeck == "Artic")
            firstCardDeck = "Arctic";

        var solution = fldSolution.Get();

        if (solution == 0)
        {
            Debug.LogFormat("[Souvenir #{0}] No question for The Deck of Many Things because the solution was the first card.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        addQuestion(module, Question.DeckOfManyThingsFirstCard, correctAnswers: new[] { firstCardDeck });
    }

    private IEnumerable<object> ProcessDecoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "DecoloredSquaresModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DecoloredSquares);

        var colColor = GetField<string>(comp, "_color1").Get();
        var rowColor = GetField<string>(comp, "_color2").Get();

        addQuestions(module,
            makeQuestion(Question.DecoloredSquaresStartingPos, _DecoloredSquares, new[] { "column" }, new[] { colColor }),
            makeQuestion(Question.DecoloredSquaresStartingPos, _DecoloredSquares, new[] { "row" }, new[] { rowColor }));
    }

    private IEnumerable<object> ProcessDiscoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "DiscoloredSquaresModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DiscoloredSquares);

        var colorsRaw = GetField<Array>(comp, "_rememberedColors").Get(arr => arr.Length != 4 ? "expected length 4" : null);
        var positions = GetArrayField<int>(comp, "_rememberedPositions").Get(expectedLength: 4);
        var colors = colorsRaw.Cast<object>().Select(obj => obj.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[0] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites[positions[0]] }),
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[1] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites[positions[1]] }),
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[2] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites[positions[2]] }),
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[3] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites[positions[3]] }));
    }

    private IEnumerable<object> ProcessDoubleColor(KMBombModule module)
    {
        var comp = GetComponent(module, "doubleColor");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldColor = GetIntField(comp, "screenColor");
        var fldStage = GetIntField(comp, "stageNumber");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var color1 = fldColor.Get(min: 0, max: 4);
        var stage = fldStage.Get(min: 1, max: 1);
        var submitBtn = GetField<KMSelectable>(comp, "submit", isPublic: true).Get();

        var prevInteract = submitBtn.OnInteract;
        submitBtn.OnInteract = delegate
        {
            var ret = prevInteract();
            stage = fldStage.Get();
            if (stage == 1)  // This means the user got a strike. Need to retrieve the new first stage color
                // We mustn’t throw an exception inside of the button handler, so don’t check min/max values here
                color1 = fldColor.Get();
            return ret;
        };

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DoubleColor);

        // Check the value of color1 because we might have reassigned it inside the button handler
        if (color1 < 0 || color1 > 4)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Double Color because first stage color has unexpected value: {1} (expected 0 to 4).", _moduleId, color1);
            yield break;
        }

        var color2 = fldColor.Get(min: 0, max: 4);

        var colorNames = new[] { "Green", "Blue", "Red", "Pink", "Yellow" };

        addQuestions(module,
            makeQuestion(Question.DoubleColorColors, _DoubleColor, new[] { "first" }, new[] { colorNames[color1] }),
            makeQuestion(Question.DoubleColorColors, _DoubleColor, new[] { "second" }, new[] { colorNames[color2] }));
    }

    private IEnumerable<object> ProcessDoubleOh(KMBombModule module)
    {
        var comp = GetComponent(module, "DoubleOhModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DoubleOh);

        var submitIndex = GetField<Array>(comp, "_functions").Get().Cast<object>().IndexOf(f => f.ToString() == "Submit");
        if (submitIndex < 0 || submitIndex > 4)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Double-Oh because submit button is at index {1} (expected 0–4).", _moduleId, submitIndex);
            yield break;
        }

        addQuestion(module, Question.DoubleOhSubmitButton, correctAnswers: new[] { "↕↔⇔⇕◆".Substring(submitIndex, 1) });
    }

    private IEnumerable<object> ProcessDrDoctor(KMBombModule module)
    {
        var comp = GetComponent(module, "DrDoctorModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DrDoctor);

        var diagnoses = GetArrayField<string>(comp, "_selectableDiagnoses").Get();
        var symptoms = GetArrayField<string>(comp, "_selectableSymptoms").Get();
        var diagnoseText = GetField<TextMesh>(comp, "DiagnoseText", isPublic: true).Get();

        addQuestions(module,
            makeQuestion(Question.DrDoctorDiseases, _DrDoctor, correctAnswers: diagnoses.Except(new[] { diagnoseText.text }).ToArray()),
            makeQuestion(Question.DrDoctorSymptoms, _DrDoctor, correctAnswers: symptoms));
    }

    private IEnumerable<object> ProcessElderFuthark(KMBombModule module)
    {
        var comp = GetComponent(module, "ElderFutharkScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ElderFuthark);

        var pickedRuneNames = GetArrayField<string>(comp, "pickedRuneNames").Get(expectedLength: 2);

        addQuestions(module,
            makeQuestion(Question.ElderFutharkRunes, _ElderFuthark, correctAnswers: new[] { pickedRuneNames[0] }, formatArgs: new[] { "first" }, preferredWrongAnswers: pickedRuneNames),
            makeQuestion(Question.ElderFutharkRunes, _ElderFuthark, correctAnswers: new[] { pickedRuneNames[1] }, formatArgs: new[] { "second" }, preferredWrongAnswers: pickedRuneNames));
    }

    private IEnumerable<object> ProcessEncryptedEquations(KMBombModule module)
    {
        var comp = GetComponent(module, "EncryptedEquations");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_EncryptedEquations);

        var equation = GetField<object>(comp, "CurrentEquation").Get();

        addQuestions(module, new[] { "LeftOperand", "MiddleOperand", "RightOperand" }
            .Select(fldName => GetField<object>(equation, fldName, isPublic: true).Get())
            .Select(op => GetField<object>(op, "Shape", isPublic: true).Get())
            .Select(sh => GetIntField(sh, "TextureIndex", isPublic: true).Get(0, EncryptedEquationsSprites.Length - 1))
            .Select((txIx, opIx) => txIx == -1 ? null : new { Shape = EncryptedEquationsSprites[txIx], Ordinal = ordinal(opIx + 1) })
            .Where(inf => inf != null)
            .Select(inf => makeQuestion(Question.EncryptedEquationsShapes, _EncryptedEquations, formatArgs: new[] { inf.Ordinal }, correctAnswers: new[] { inf.Shape }, preferredWrongAnswers: EncryptedEquationsSprites)));
    }

    private IEnumerable<object> ProcessEncryptedHangman(KMBombModule module)
    {
        var comp = GetComponent(module, "HangmanScript");
        var fldSolved = GetField<bool>(comp, "isSolved", isPublic: true);

        var moduleName = GetField<string>(comp, "moduleName", isPublic: true).Get();
        if (moduleName.Length == 0)
            throw new AbandonModuleException("‘moduleName’ is empty.");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_EncryptedHangman);

        var wrongModuleNames = Bomb.GetSolvableModuleNames();
        // If there are less than 4 eligible modules, fill the remaining spaces with random other modules.
        if (wrongModuleNames.Count < 4)
            wrongModuleNames.AddRange(_attributes.Where(x => x.Value != null).Select(x => x.Value.ModuleNameWithThe).Distinct());

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.EncryptedHangmanModule, _EncryptedHangman, correctAnswers: new[] { moduleName }, preferredWrongAnswers: wrongModuleNames.ToArray()));

        var encryptionMethodNames = new[] { "Caesar Cipher", "Playfair Cipher", "Rot-13 Cipher", "Atbash Cipher", "Affine Cipher", "Modern Cipher", "Vigenère Cipher" };
        var encryptionMethod = GetIntField(comp, "encryptionMethod").Get(0, encryptionMethodNames.Length - 1);
        qs.Add(makeQuestion(Question.EncryptedHangmanEncryptionMethod, _EncryptedHangman, correctAnswers: new[] { encryptionMethodNames[encryptionMethod] }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessEncryptedMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "EncryptedMorseModule");
        var fldSolved = GetField<bool>(comp, "solved");

        string[] formatCalls = { "Detonate", "Ready Now", "We're Dead", "She Sells", "Remember", "Great Job", "Solo This", "Keep Talk" };
        string[] formatResponses = { "Please No", "Cheesecake", "Sadface", "Sea Shells", "Souvenir", "Thank You", "I Dare You", "No Explode" };
        int index = GetIntField(comp, "callResponseIndex").Get(0, Math.Min(formatCalls.Length - 1, formatResponses.Length - 1));

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_EncryptedMorse);
        addQuestions(module,
            makeQuestion(Question.EncryptedMorseCallResponse, _EncryptedMorse, new[] { "received call" }, new[] { formatCalls[index] }, formatCalls),
            makeQuestion(Question.EncryptedMorseCallResponse, _EncryptedMorse, new[] { "sent response" }, new[] { formatResponses[index] }, formatResponses));
    }

    private IEnumerable<object> ProcessEquationsX(KMBombModule module)
    {
        var comp = GetComponent(module, "EquationsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var symbol = GetField<GameObject>(comp, "symboldisplay", isPublic: true).Get().GetComponentInChildren<TextMesh>().text;

        if (!new[] { "H(T)", "R", "\u03C7", "w", "Z(T)", "t", "m", "a", "K" }.Contains(symbol))
            throw new AbandonModuleException("‘symbol’ has an unexpected character: {0}", symbol);

        // Equations X uses symbols that don’t translate well to Souvenir. This switch statement is used to correctly translate the answer.
        switch (symbol)
        {
            case "R":
                symbol = "P";
                break;
            case "w":
                symbol = "\u03C9";
                break;
            case "t":
                symbol = "\u03C4";
                break;
            case "m":
                symbol = "\u03BC";
                break;
            case "a":
                symbol = "\u03B1";
                break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_EquationsX);

        addQuestion(module, Question.EquationsXSymbols, correctAnswers: new[] { symbol });
    }

    private IEnumerable<object> ProcessEtterna(KMBombModule module)
    {
        var comp = GetComponent(module, "Etterna");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Etterna);

        var correct = GetArrayField<byte>(comp, "correct").Get(expectedLength: 4, validator: b => b > 32 || b == 0 ? "expected 1–32" : null);
        addQuestions(module, correct.Select((answer, ix) => makeQuestion(Question.EtternaNumber, _Etterna, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { answer.ToString() })));
    }

    private IEnumerable<object> ProcessFactoryMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "FactoryMazeScript");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_FactoryMaze);

        var usedRooms = GetArrayField<string>(comp, "usedRooms").Get(expectedLength: 5).ToArray();
        var startRoom = GetIntField(comp, "startRoom").Get(0, usedRooms.Length - 1);

        for (int i = usedRooms.Length - 1; i >= 0; --i)
            usedRooms[i] = usedRooms[i].Replace('\n', ' ');

        addQuestion(module, Question.FactoryMazeStartRoom, correctAnswers: new[] { usedRooms[startRoom] }, preferredWrongAnswers: usedRooms);
    }

    private IEnumerable<object> ProcessFastMath(KMBombModule module)
    {
        var comp = GetComponent(module, "FastMathModule");
        var fldScreen = GetField<TextMesh>(comp, "Screen", isPublic: true);
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var prevLetters = new HashSet<string>();
        string letters = null;
        while (!fldSolved.Get())
        {
            var display = fldScreen.Get().text;
            if (display.Length != 3)
                throw new AbandonModuleException("The screen contains something other than three characters: “{0}” ({1} characters).", display, display.Length);
            letters = display[0] + "" + display[2];
            prevLetters.Add(letters);
            yield return new WaitForSeconds(.1f);
        }
        if (letters == null)
            throw new AbandonModuleException("No letters were extracted before the module was solved.");

        _modulesSolved.IncSafe(_FastMath);
        addQuestion(module, Question.FastMathLastLetters, correctAnswers: new[] { letters }, preferredWrongAnswers: prevLetters.ToArray());
    }

    private IEnumerable<object> ProcessFaultyRGBMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "FaultyRGBMazeScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_FaultyRGBMaze);

        var keyPos = GetArrayField<int[]>(comp, "keylocations").Get(expectedLength: 3, validator: key => key.Length != 2 ? "expected length 2" : key.Any(number => number < 0 || number > 6) ? "expected range 0–6" : null);
        var mazeNum = GetArrayField<int[]>(comp, "mazenumber").Get(expectedLength: 3, validator: maze => maze.Length != 2 ? "expected length 2" : maze[0] < 0 || maze[0] > 15 ? "expected range 0–15" : null);
        var exitPos = GetArrayField<int>(comp, "exitlocation").Get(expectedLength: 3);

        if (exitPos[1] < 0 || exitPos[1] > 6 || exitPos[2] < 0 || exitPos[2] > 6)
            throw new AbandonModuleException("‘exitPos’ contains invalid coordinate: ({0},{1})", exitPos[2], exitPos[1]);

        string[] colors = { "red", "green", "blue" };

        var qs = new List<QandA>();

        for (int index = 0; index < 3; index++)
        {
            qs.Add(makeQuestion(Question.FaultyRGBMazeKeys, _FaultyRGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { "ABCDEFG"[keyPos[index][1]] + (keyPos[index][0] + 1).ToString() }));
            qs.Add(makeQuestion(Question.FaultyRGBMazeNumber, _FaultyRGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { "0123456789abcdef"[mazeNum[index][0]].ToString() }));
        }

        qs.Add(makeQuestion(Question.FaultyRGBMazeExit, _FaultyRGBMaze,
            correctAnswers: new[] { "ABCDEFG"[exitPos[2]] + (exitPos[1] + 1).ToString() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessFlags(KMBombModule module)
    {
        var comp = GetComponent(module, "FlagsModule");
        var fldCanInteract = GetField<bool>(comp, "canInteract");
        var mainCountry = GetField<object>(comp, "mainCountry").Get();
        var countries = GetListField<IList>(comp, "countries").Get();
        var number = GetIntField(comp, "number").Get(1, 7);

        if (countries.Count != 7)
            throw new AbandonModuleException("‘countries’ has length {0} (expected 7).", countries.Count);

        var propCountryName = GetProperty<string>(mainCountry, "CountryName", isPublic: true);
        var mainCountrySprite = FlagsSprites.FirstOrDefault(spr => spr.name == propCountryName.GetFrom(mainCountry));
        var otherCountrySprites = countries.Cast<object>().Select(country => FlagsSprites.FirstOrDefault(spr => spr.name == propCountryName.GetFrom(country))).ToArray();

        if (mainCountrySprite == null || otherCountrySprites.Any(spr => spr == null))
            throw new AbandonModuleException("Abandoning Flags because one of the countries has a name with no corresponding sprite: main country = {0}, other countries = [{1}].", propCountryName.GetFrom(mainCountry), countries.Cast<object>().Select(country => propCountryName.GetFrom(country)).JoinString(", "));

        while (fldCanInteract.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Flags);

        addQuestions(module,
            // Displayed number
            makeQuestion(Question.FlagsDisplayedNumber, _Flags, correctAnswers: new[] { number.ToString() }),
            // Main country flag
            makeQuestion(Question.FlagsMainCountry, _Flags, correctAnswers: new[] { mainCountrySprite }, preferredWrongAnswers: otherCountrySprites),
            // Rest of the country flags
            makeQuestion(Question.FlagsCountries, _Flags, correctAnswers: otherCountrySprites, preferredWrongAnswers: FlagsSprites));
    }

    private IEnumerable<object> ProcessFlashingLights(KMBombModule module)
    {
        var comp = GetComponent(module, "doubleNegativesScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_FlashingLights);

        var topColors = GetListField<int>(comp, "selectedColours").Get(expectedLength: 12);
        var bottomColors = GetListField<int>(comp, "selectedColours2").Get(expectedLength: 12);
        var colorNames = new[] { "blue", "green", "red", "purple", "orange" };
        var topTotals = Enumerable.Range(1, 5).Select(num => topColors.Count(x => x == num)).ToArray();
        var bottomTotals = Enumerable.Range(1, 5).Select(num => bottomColors.Count(x => x == num)).ToArray();

        var qs = new List<QandA>();
        for (int i = 0; i < 5; i++)
        {
            qs.Add(makeQuestion(Question.FlashingLightsLEDFrequency, _FlashingLights, new[] { "top", colorNames[i] }, new[] { topTotals[i].ToString() }, new[] { bottomTotals[i].ToString() }));
            qs.Add(makeQuestion(Question.FlashingLightsLEDFrequency, _FlashingLights, new[] { "bottom", colorNames[i] }, new[] { bottomTotals[i].ToString() }, new[] { topTotals[i].ToString() }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessForgetTheColors(KMBombModule module)
    {
        var comp = GetComponent(module, "FTC");
        var fldStage = GetIntField(comp, "stage");

        int ftcCount;
        if (_moduleCounts.TryGetValue(_ForgetTheColors, out ftcCount) && ftcCount > 1)
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Forget The Colors because there is more than one of them.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        var maxStage = GetIntField(comp, "maxStage").Get();
        var stage = fldStage.Get();
        var gear = GetListField<byte>(comp, "gear").Get();
        var largeDisplay = GetListField<short>(comp, "largeDisplay").Get();
        var sineNumber = GetListField<int>(comp, "sineNumber").Get();
        var gearColor = GetListField<string>(comp, "gearColor").Get();
        var ruleColor = GetListField<string>(comp, "ruleColor").Get();

        if (maxStage < stage)
            throw new AbandonModuleException("‘stage’ had an unexpected value: expected 0-{0}, was {1}.", maxStage, stage);

        string[] colors = { "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Purple", "Pink", "Maroon", "White", "Gray" };

        var randomStage = 0;
        // Uncomment the line below if you want the module to pick a random stage instead of only stage 0.
        // var randomStage = Rnd.Range(0, Math.Min(maxStage, _coroutinesActive)) % 100;
        Debug.LogFormat("<Souvenir #{0}> Waiting for stage {1} of ForgetTheColors.", _moduleId, randomStage);
        while (fldStage.Get() <= randomStage)
            yield return null;  // Don’t wait .1 seconds so that we are absolutely sure we get the right stage
        _modulesSolved.IncSafe(_ForgetTheColors);

        if (gear.Count <= randomStage || largeDisplay.Count <= randomStage || sineNumber.Count <= randomStage || gearColor.Count <= randomStage || ruleColor.Count <= randomStage)
            throw new AbandonModuleException("One or more of the lists have an unexpected level of entries. (Expected less than or equal {1}): Gear: {2}, LargeDisplay: {3}, SineNumber: {4}, GearColor: {5}, RuleColor: {6}", _moduleId, randomStage, gear.Count, largeDisplay.Count, sineNumber.Count, gearColor.Count, ruleColor.Count);

        if (!new[] { gear.Count, largeDisplay.Count, sineNumber.Count, gearColor.Count, ruleColor.Count }.All(x => x == gear.Count))
            throw new AbandonModuleException("One or more of the lists aren't all the same length. (Expected {1}): Gear: {1}, LargeDisplay: {2}, SineNumber: {3}, GearColor: {4}, RuleColor: {5}", _moduleId, gear.Count, largeDisplay.Count, sineNumber.Count, gearColor.Count, ruleColor.Count);

        for (int i = 0; i < gear.Count; i++)
        {
            if (gear[i] < 0 || gear[i] > 9)
                throw new AbandonModuleException("‘gear[{0}]’ had an unexpected value. (Expected 0-9): {1}", i, gear[i]);
            if (largeDisplay[i] < 0 || largeDisplay[i] > 990)
                throw new AbandonModuleException("‘largeDisplay[{0}]’ had an unexpected value. (Expected 0-990): {1}", i, largeDisplay[i]);
            if (sineNumber[i] < -99999 || sineNumber[i] > 99999)
                throw new AbandonModuleException("‘sineNumber[{0}]’ had an unexpected value. (Expected (-99999)-99999): {1}", i, sineNumber[i]);
            if (!colors.Contains(gearColor[i]))
                throw new AbandonModuleException("‘gearColor[{0}]’ had an unexpected value. (Expected {1}): {2}", i, colors.JoinString(", "), sineNumber[i]);
            if (!colors.Contains(ruleColor[i]))
                throw new AbandonModuleException("‘ruleColor[{0}]’ had an unexpected value. (Expected {1}): {2}", i, colors.JoinString(", "), ruleColor[i]);
        }

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.ForgetTheColorsGearNumber, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { gear[randomStage].ToString() }, preferredWrongAnswers: new[] { Rnd.Range(0, 10).ToString() }));
        qs.Add(makeQuestion(Question.ForgetTheColorsLargeDisplay, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { largeDisplay[randomStage].ToString() }, preferredWrongAnswers: new[] { Rnd.Range(0, 991).ToString() }));
        qs.Add(makeQuestion(Question.ForgetTheColorsSineNumber, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { (Mathf.Abs(sineNumber[randomStage]) % 10).ToString() }, preferredWrongAnswers: new[] { Rnd.Range(0, 10).ToString() }));
        qs.Add(makeQuestion(Question.ForgetTheColorsGearColor, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { gearColor[randomStage].ToString() }, preferredWrongAnswers: new[] { colors[Rnd.Range(0, colors.Length)] }));
        qs.Add(makeQuestion(Question.ForgetTheColorsRuleColor, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { ruleColor[randomStage].ToString() }, preferredWrongAnswers: new[] { colors[Rnd.Range(0, colors.Length)] }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessFreeParking(KMBombModule module)
    {
        var comp = GetComponent(module, "FreeParkingScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        var tokens = GetArrayField<Material>(comp, "tokenOptions", isPublic: true).Get(expectedLength: 7);
        var selected = GetIntField(comp, "tokenIndex").Get(0, tokens.Length - 1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_FreeParking);
        addQuestion(module, Question.FreeParkingToken, correctAnswers: new[] { tokens[selected].name });
    }

    private IEnumerable<object> ProcessFunctions(KMBombModule module)
    {
        var comp = GetComponent(module, "qFunctions");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Functions);

        var lastDigit = GetIntField(comp, "firstLastDigit").Get(-1, 9);
        if (lastDigit == -1)
        {
            Debug.LogFormat("[Souvenir #{0}] No questions for Functions because it was solved with no queries! This isn’t a bug, just impressive (or cheating).", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        var lNum = GetIntField(comp, "numberA").Get(1, 999);
        var rNum = GetIntField(comp, "numberB").Get(1, 999);
        var theLetter = GetField<string>(comp, "ruleLetter").Get(s => s.Length != 1 ? "expected length 1" : null);

        addQuestions(module,
            makeQuestion(Question.FunctionsLastDigit, _Functions, correctAnswers: new[] { lastDigit.ToString() }),
            makeQuestion(Question.FunctionsLeftNumber, _Functions, correctAnswers: new[] { lNum.ToString() }, preferredWrongAnswers:
                Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(1, 999).ToString()).Distinct().Take(6).ToArray()),
            makeQuestion(Question.FunctionsLetter, _Functions, correctAnswers: new[] { theLetter }),
            makeQuestion(Question.FunctionsRightNumber, _Functions, correctAnswers: new[] { rNum.ToString() }, preferredWrongAnswers:
                Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(1, 999).ToString()).Distinct().Take(6).ToArray()));
    }

    private IEnumerable<object> ProcessGamepad(KMBombModule module)
    {
        var comp = GetComponent(module, "GamepadModule");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.05f);
        _modulesSolved.IncSafe(_Gamepad);

        var x = GetIntField(comp, "x").Get(min: 1, max: 99);
        var y = GetIntField(comp, "y").Get(min: 1, max: 99);
        var display = GetField<GameObject>(comp, "Input", isPublic: true).Get().GetComponent<TextMesh>();
        var digits1 = GetField<GameObject>(comp, "Digits1", isPublic: true).Get().GetComponent<TextMesh>();
        var digits2 = GetField<GameObject>(comp, "Digits2", isPublic: true).Get().GetComponent<TextMesh>();

        if (display == null || digits1 == null || digits2 == null)
            throw new AbandonModuleException("One of the three displays does not have a TextMesh ({0}, {1}, {2}).",
                display == null ? "null" : "not null", digits1 == null ? "null" : "not null", digits2 == null ? "null" : "not null");

        addQuestions(module, makeQuestion(Question.GamepadNumbers, _Gamepad, correctAnswers: new[] { string.Format("{0:00}:{1:00}", x, y) },
            preferredWrongAnswers: Enumerable.Range(0, int.MaxValue).Select(i => string.Format("{0:00}:{1:00}", Rnd.Range(1, 99), Rnd.Range(1, 99))).Distinct().Take(6).ToArray()));
        digits1.GetComponent<TextMesh>().text = "--";
        digits2.GetComponent<TextMesh>().text = "--";
    }

    private IEnumerable<object> ProcessGiantsDrink(KMBombModule module)
    {
        var comp = GetComponent(module, "giantsDrinkScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return null;  // Don’t wait .1 seconds so we get the number of strikes exactly at the time of solve
        _modulesSolved.IncSafe(_GiantsDrink);

        var liquids = GetArrayField<int>(comp, "liquid").Get(expectedLength: 2);
        var sol = GetIntField(comp, Bomb.GetStrikes() % 2 == 0 ? "evenStrikes" : "oddStrikes").Get(0, liquids.Length - 1);
        var liquidNames = new[] { "Red", "Blue", "Green", "Orange", "Purple", "Cyan" };
        addQuestion(module, Question.GiantsDrinkLiquid, correctAnswers: new[] { liquidNames[liquids[sol]] });
    }

    private IEnumerable<object> ProcessGreenArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "GreenArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldNumDisplay = GetField<GameObject>(comp, "numDisplay", isPublic: true);
        var fldStreak = GetIntField(comp, "streak");
        var fldAnimating = GetField<bool>(comp, "isanimating");

        string numbers = null;
        bool activated = false;
        while (!fldSolved.Get())
        {
            int streak = fldStreak.Get();
            bool animating = fldAnimating.Get();
            if (streak == 6 && !animating && !activated)
            {
                var numDisplay = fldNumDisplay.Get();
                if (numDisplay == null)
                    yield break;
                numbers = numDisplay.GetComponent<TextMesh>().text;
                if (numbers == null)
                    yield break;
                activated = true;
            }
            if (streak == 0)
                activated = false;
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_GreenArrows);

        int number;
        if (!int.TryParse(numbers, out number))
            throw new AbandonModuleException("The screen is not an integer: “{0}”.", number);
        if (number < 0 || number > 99)
            throw new AbandonModuleException("The number on the screen is out of range: number = {1}, expected 0-99", number);

        addQuestions(module, makeQuestion(Question.GreenArrowsLastScreen, _GreenArrows, correctAnswers: new[] { number.ToString() }));
    }

    private IEnumerable<object> ProcessGridLock(KMBombModule module)
    {
        var comp = GetComponent(module, "GridlockModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        var colors = GetAnswers(Question.GridLockStartingColor);
        if (colors == null)
            yield break;

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var solution = GetIntField(comp, "_solution").Get(min: 0, max: 15);
        var pages = GetArrayField<int[]>(comp, "_pages").Get(minLength: 5, maxLength: 10, validator: p => p.Length != 16 ? "expected length 16" : p.Any(q => q < 0 || (q & 15) > 12 || (q & (15 << 4)) > (4 << 4)) ? "unexpected value" : null);
        var start = pages[0].IndexOf(i => (i & 15) == 4);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_GridLock);
        addQuestions(module,
            makeQuestion(Question.GridLockStartingLocation, _GridLock, correctAnswers: new[] { ((char) ('A' + start % 4)).ToString() + (char) ('1' + start / 4) }),
            makeQuestion(Question.GridLockEndingLocation, _GridLock, correctAnswers: new[] { ((char) ('A' + solution % 4)).ToString() + (char) ('1' + solution / 4) }),
            makeQuestion(Question.GridLockStartingColor, _GridLock, correctAnswers: new[] { colors[(pages[0][start] >> 4) - 1] }));
    }

    private IEnumerable<object> ProcessGryphons(KMBombModule module)
    {
        var comp = GetComponent(module, "Gryphons");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Gryphons);

        var age = GetIntField(comp, "age").Get(23, 34);
        var name = GetField<string>(comp, "theirName").Get();

        addQuestions(module,
            makeQuestion(Question.GryphonsName, _Gryphons, correctAnswers: new[] { name }),
            makeQuestion(Question.GryphonsAge, _Gryphons, correctAnswers: new[] { age.ToString() }, preferredWrongAnswers:
                Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(23, 34).ToString()).Distinct().Take(6).ToArray()));
    }

    private IEnumerable<object> ProcessHereditaryBaseNotation(KMBombModule module)
    {
        var comp = GetComponent(module, "hereditaryBaseNotationScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var mthNumberToBaseNString = GetMethod<string>(comp, "numberToBaseNString", numParameters: 2);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HereditaryBaseNotation);

        int baseN = GetIntField(comp, "baseN").Get(3, 7);
        int upperBound = new[] { 19682, 60000, 80000, 100000, 100000 }[baseN - 3];
        int initialNum = GetIntField(comp, "initialNumber").Get(1, upperBound);

        string answer = mthNumberToBaseNString.Invoke(baseN, initialNum).ToString();
        HashSet<string> invalidAnswer = new HashSet<string>();
        invalidAnswer.Add(answer);

        //Generate fake options in the same base of the answer.
        while (invalidAnswer.Count() < 4)
        {
            var wrongAnswer = Rnd.Range(1, upperBound + 1);
            invalidAnswer.Add(mthNumberToBaseNString.Invoke(baseN, wrongAnswer).ToString());
        }

        invalidAnswer.Add(answer);
        addQuestions(module, makeQuestion(Question.HereditaryBaseNotationInitialNumber, _HereditaryBaseNotation, null, new[] { answer }, invalidAnswer.ToArray()));
    }

    private IEnumerable<object> ProcessHexabutton(KMBombModule module)
    {
        var comp = GetComponent(module, "hexabuttonScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var labels = GetArrayField<string>(comp, "labels").Get();
        var index = GetIntField(comp, "labelNum").Get(0, labels.Length - 1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Hexabutton);
        addQuestion(module, Question.HexabuttonLabel, correctAnswers: new[] { labels[index] });
    }

    private IEnumerable<object> ProcessHexamaze(KMBombModule module)
    {
        var comp = GetComponent(module, "HexamazeModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        if (comp == null | GetIntField(comp, "_pawnColor") == null || fldSolved == null)
            yield break;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Hexamaze);
        addQuestion(module, Question.HexamazePawnColor, correctAnswers: new[] { new[] { "Red", "Yellow", "Green", "Cyan", "Blue", "Pink" }[GetIntField(comp, "_pawnColor").Get(0, 5)] });
    }

    private IEnumerable<object> ProcessHexOS(KMBombModule module)
    {
        var comp = GetComponent(module, "HexOS");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HexOS);

        const string validCharacters = " ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var decipher = GetArrayField<char>(comp, "decipher").Get(expectedLength: 2, validator: ch => !validCharacters.Contains(ch) ? "expected characters A–Z or space" : null);
        var screen = GetField<string>(comp, "screen").Get(s => s.Length != 30 ? "expected length 30" : s.Any(ch => !char.IsDigit(ch)) ? "expected only digits" : null);
        var sum = GetField<string>(comp, "sum").Get(s => s.Length != 4 ? "expected length 4" : s.Any(ch => ch != '0' && ch != '1' && ch != '2') ? "expected only characters 0–2" : null);

        var qs = new List<QandA>();
        var possibleWrongAnswers = validCharacters.SelectMany(c1 => validCharacters.Select(c2 => string.Concat(c1, c2))).ToArray();
        qs.Add(makeQuestion(Question.HexOSCipher, _HexOS, correctAnswers: new[] { decipher.JoinString(), decipher.Reverse().JoinString() }, preferredWrongAnswers: possibleWrongAnswers));
        qs.Add(makeQuestion(Question.HexOSSum, _HexOS, correctAnswers: new[] { sum }));
        for (var offset = 0; offset < 10; offset++)
            qs.Add(makeQuestion(Question.HexOSScreen, _HexOS, new[] { ordinal(offset) }, correctAnswers: new[] { screen.Substring(offset * 3, 3) }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessHiddenColors(KMBombModule module)
    {
        var comp = GetComponent(module, "HiddenColorsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        var ledcolors = new[] { "Red", "Blue", "Green", "Yellow", "Orange", "Purple", "Magenta", "White" };
        var ledcolor = GetIntField(comp, "LEDColor").Get(min: 0, max: 7);
        var colors = GetArrayField<Material>(comp, "buttonColors", isPublic: true).Get();
        var led = GetField<Renderer>(comp, "LED", isPublic: true).Get();

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HiddenColors);

        if (colors.Length == 9)
            led.material = colors[8];
        addQuestion(module, Question.HiddenColorsLED, null, new[] { ledcolors[ledcolor] });
    }

    private IEnumerable<object> ProcessHillCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "HillCycleScript", Question.HillCycleWord, _HillCycle);
    }

    private IEnumerable<object> ProcessHogwarts(KMBombModule module)
    {
        var comp = GetComponent(module, "HogwartsModule");
        var fldModuleNames = GetField<IDictionary>(comp, "_moduleNames");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Hogwarts);

        var dic = fldModuleNames.Get(d => d.Count == 0 ? "empty" : null);

        // Rock-Paper-Scissors-Lizard-Spock needs to be broken up in the question because hyphens don't word-wrap.
        addQuestions(module,
            dic.Keys.Cast<object>().Where(house => dic[house] != null).SelectMany(house => Ut.NewArray(
                makeQuestion(Question.HogwartsHouse, _Hogwarts,
                    formatArgs: new[] { dic[house].ToString() == "Rock-Paper-Scissors-L.-Sp." ? "Rock-Paper- Scissors-L.-Sp." : dic[house].ToString() },
                    correctAnswers: new[] { house.ToString() }),
                makeQuestion(Question.HogwartsModule, _Hogwarts,
                    formatArgs: new[] { house.ToString() },
                    correctAnswers: new[] { dic[house].ToString() },
                    preferredWrongAnswers: Bomb.GetSolvableModuleNames().ToArray()))));
    }

    private IEnumerable<object> ProcessHorribleMemory(KMBombModule module)
    {
        var comp = GetComponent(module, "cruelMemoryScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HorribleMemory);

        var pos = GetListField<int>(comp, "correctStagePositions", isPublic: true).Get(expectedLength: 5);
        var lbl = GetListField<int>(comp, "correctStageLabels", isPublic: true).Get(expectedLength: 5);
        var colors = GetListField<string>(comp, "correctStageColours", isPublic: true).Get(expectedLength: 5);

        addQuestions(module,
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "first" }, new[] { pos[0].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "second" }, new[] { pos[1].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "third" }, new[] { pos[2].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "fourth" }, new[] { pos[3].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "fifth" }, new[] { pos[4].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "first" }, new[] { lbl[0].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "second" }, new[] { lbl[1].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "third" }, new[] { lbl[2].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "fourth" }, new[] { lbl[3].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "fifth" }, new[] { lbl[4].ToString() }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "first" }, new[] { colors[0] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "second" }, new[] { colors[1] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "third" }, new[] { colors[2] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "fourth" }, new[] { colors[3] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "fifth" }, new[] { colors[4] }));
    }

    private IEnumerable<object> ProcessHumanResources(KMBombModule module)
    {
        var comp = GetComponent(module, "HumanResourcesModule");
        var people = GetStaticField<Array>(comp.GetType(), "_people").Get(ar => ar.Length != 16 ? "expected length 16" : null);
        var fldSolved = GetField<bool>(comp, "_isSolved");

        var person = people.GetValue(0);
        var fldName = GetField<string>(person, "Name", isPublic: true);
        var fldDesc = GetField<string>(person, "Descriptor", isPublic: true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HumanResources);

        var names = GetArrayField<int>(comp, "_availableNames").Get(expectedLength: 10);
        var descs = GetArrayField<int>(comp, "_availableDescs").Get(expectedLength: 5);
        var toHire = GetIntField(comp, "_personToHire").Get();
        var toFire = GetIntField(comp, "_personToFire").Get();

        addQuestions(module,
            makeQuestion(Question.HumanResourcesEmployees, _HumanResources, new[] { "an employee that was not fired" }, names.Take(5).Where(ix => ix != toFire).Select(ix => fldName.GetFrom(people.GetValue(ix))).ToArray()),
            makeQuestion(Question.HumanResourcesEmployees, _HumanResources, new[] { "an applicant that was not hired" }, names.Skip(5).Where(ix => ix != toHire).Select(ix => fldName.GetFrom(people.GetValue(ix))).ToArray()),
            makeQuestion(Question.HumanResourcesDescriptors, _HumanResources, new[] { "red" }, descs.Take(3).Select(ix => fldDesc.GetFrom(people.GetValue(ix))).ToArray()),
            makeQuestion(Question.HumanResourcesDescriptors, _HumanResources, new[] { "green" }, descs.Skip(3).Select(ix => fldDesc.GetFrom(people.GetValue(ix))).ToArray()));
    }

    private IEnumerable<object> ProcessHunting(KMBombModule module)
    {
        var comp = GetComponent(module, "hunting");
        var fldStage = GetIntField(comp, "stage");
        var fldReverseClues = GetField<bool>(comp, "reverseClues");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var hasRowFirst = new bool[4];
        while (fldStage.Get() < 5)
        {
            hasRowFirst[fldStage.Get() - 1] = fldReverseClues.Get();
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_Hunting);
        var qs = new List<QandA>();
        foreach (var row in new[] { false, true })
            foreach (var first in new[] { false, true })
                qs.Add(makeQuestion(Question.HuntingColumnsRows, _Hunting,
                    formatArgs: new[] { row ? "row" : "column", first ? "first" : "second" },
                    correctAnswers: new[] { _attributes[Question.HuntingColumnsRows].AllAnswers[(hasRowFirst[0] ^ row ^ first ? 1 : 0) | (hasRowFirst[1] ^ row ^ first ? 2 : 0) | (hasRowFirst[2] ^ row ^ first ? 4 : 0)] }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessHypercube(KMBombModule module)
    {
        return processHypercubeUltracube(module, "TheHypercubeModule", Question.HypercubeRotations, _Hypercube);
    }

    private IEnumerable<object> ProcessIceCream(KMBombModule module)
    {
        var comp = GetComponent(module, "IceCreamModule");
        var fldCurrentStage = GetIntField(comp, "CurrentStage");
        var fldCustomers = GetArrayField<int>(comp, "CustomerNamesSolution");
        var fldSolution = GetArrayField<int>(comp, "Solution");
        var fldFlavourOptions = GetArrayField<int[]>(comp, "FlavorOptions");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var flavourNames = GetAnswers(Question.IceCreamFlavour);
        var customerNames = GetAnswers(Question.IceCreamCustomer);

        var flavours = new int[3][];
        var solution = new int[3];
        var customers = new int[3];

        for (var i = 0; i < 3; i++)
        {
            while (fldCurrentStage.Get() == i)
                yield return new WaitForSeconds(.1f);
            if (fldCurrentStage.Get() < i)
                throw new AbandonModuleException("The stage number went down from {0} to {1}.", i, fldCurrentStage.Get());

            var options = fldFlavourOptions.Get(expectedLength: 3, validator: x => x.Length != 5 ? "expected length 5" : x.Any(y => y < 0 || y >= flavourNames.Length) ? string.Format("expected range 0–{0}", flavourNames.Length - 1) : null);
            var sol = fldSolution.Get(ar => ar.Any(x => x < 0 || x >= flavourNames.Length) ? string.Format("expected range 0–{0}", flavourNames.Length - 1) : null);
            var cus = fldCustomers.Get(ar => ar.Any(x => x < 0 || x >= customerNames.Length) ? string.Format("expected range 0–{0}", customerNames.Length - 1) : null);

            flavours[i] = options[i].ToArray();
            solution[i] = flavours[i][sol[i]];
            customers[i] = cus[i];
        }
        var qs = new List<QandA>();
        _modulesSolved.IncSafe(_IceCream);

        for (var i = 0; i < 3; i++)
        {
            qs.Add(makeQuestion(Question.IceCreamFlavour, _IceCream, new[] { "was on offer, but not sold,", ordinal(i + 1) }, flavours[i].Where(ix => ix != solution[i]).Select(ix => flavourNames[ix]).ToArray()));
            qs.Add(makeQuestion(Question.IceCreamFlavour, _IceCream, new[] { "was not on offer", ordinal(i + 1) }, flavourNames.Where((f, ix) => !flavours[i].Contains(ix)).ToArray()));
            if (i != 2)
                qs.Add(makeQuestion(Question.IceCreamCustomer, _IceCream, new[] { ordinal(i + 1) }, new[] { customerNames[customers[i]] }, preferredWrongAnswers: customers.Select(ix => customerNames[ix]).ToArray()));
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessIdentityParade(KMBombModule module)
    {
        var comp = GetComponent(module, "identityParadeScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_IdentityParade);

        foreach (var btnFieldName in new[] { "hairLeft", "hairRight", "buildLeft", "buildRight", "attireLeft", "attireRight", "suspectLeft", "suspectRight", "convictBut" })
        {
            var btn = GetField<KMSelectable>(comp, btnFieldName, isPublic: true).Get();
            btn.OnInteract = delegate
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
                btn.AddInteractionPunch(0.5f);
                return false;
            };
        }

        var textMeshes = new[] { "hairText", "buildText", "attireText", "suspectText" }.Select(fldName => GetField<TextMesh>(comp, fldName, isPublic: true).Get()).ToArray();
        textMeshes[0].text = "Identity";
        textMeshes[1].text = "Parade";
        textMeshes[2].text = "has been";
        textMeshes[3].text = "solved";

        var hairs = GetListField<string>(comp, "hairEntries").Get(expectedLength: 3);
        var builds = GetListField<string>(comp, "buildEntries").Get(expectedLength: 3);
        var attires = GetListField<string>(comp, "attireEntries").Get(expectedLength: 3);

        var validHairs = new[] { "Black", "Blonde", "Brown", "Grey", "Red", "White" };
        var validBuilds = new[] { "Fat", "Hunched", "Muscular", "Short", "Slim", "Tall" };
        var validAttires = new[] { "Blazer", "Hoodie", "Jumper", "Suit", "T-shirt", "Tank top" };

        addQuestions(module,
            makeQuestion(Question.IdentityParadeHairColors, _IdentityParade, formatArgs: new[] { "was" }, correctAnswers: hairs.ToArray()),
            makeQuestion(Question.IdentityParadeHairColors, _IdentityParade, formatArgs: new[] { "was not" }, correctAnswers: validHairs.Except(hairs).ToArray()),
            makeQuestion(Question.IdentityParadeBuilds, _IdentityParade, formatArgs: new[] { "was" }, correctAnswers: builds.ToArray()),
            makeQuestion(Question.IdentityParadeBuilds, _IdentityParade, formatArgs: new[] { "was not" }, correctAnswers: validBuilds.Except(builds).ToArray()),
            makeQuestion(Question.IdentityParadeAttires, _IdentityParade, formatArgs: new[] { "was" }, correctAnswers: attires.ToArray()),
            makeQuestion(Question.IdentityParadeAttires, _IdentityParade, formatArgs: new[] { "was not" }, correctAnswers: validAttires.Except(attires).ToArray()));
    }

    private IEnumerable<object> ProcessInstructions(KMBombModule module)
    {
        var comp = GetComponent(module, "instructionsScript");
        var fldSolved = GetField<bool>(comp, "_solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Instructions);

        var screens = GetField<int[,]>(comp, "screens").Get();
        if (screens.GetLength(0) != 5 || screens.GetLength(1) != 2)
            throw new AbandonModuleException("‘screens’ has unexpected lengths: {0}/{1} (expected 5/2).", screens.GetLength(0), screens.GetLength(1));

        var edgeworkScreens = GetStaticField<bool[]>(comp.GetType(), "edgeworkScreens").Get(ar => ar.Length != 5 ? "expected length 5" : null);
        var edgeworkPossibilities = GetArrayField<string>(comp, "edgeworkPossibilities").Get();
        var buttonPossibilities = GetArrayField<string>(comp, "buttonPossibilities").Get();

        edgeworkPossibilities = edgeworkPossibilities.Select(ep => ep.ToLowerInvariant().Replace('\n', ' ')).ToArray();
        buttonPossibilities = buttonPossibilities.Select(bp => (bp.Length > 1 ? bp.ToLowerInvariant() : bp).Replace('\n', ' ')).ToArray();

        var qs = new List<QandA>();
        for (var btnIx = 0; btnIx < 5; btnIx++)
        {
            var ix = screens[btnIx, 0];
            var arr = edgeworkScreens[btnIx] ? edgeworkPossibilities : buttonPossibilities;
            if (ix < 0 || ix >= arr.Length)
                throw new AbandonModuleException("Abandoning Instructions because ‘screens[{0}, 0]’ has value {1} but {2} has length {3}.",
                    btnIx, ix, edgeworkScreens[btnIx] ? "edgeworkPossibilities" : "buttonPossibilities", arr.Length);
            qs.Add(makeQuestion(edgeworkScreens[btnIx] ? Question.InstructionsPhrasesEdgework : Question.InstructionsPhrasesButtons, _Instructions,
                formatArgs: new[] { ordinal(btnIx + 1) }, correctAnswers: new[] { arr[ix] }, preferredWrongAnswers: arr));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessiPhone(KMBombModule module)
    {
        var comp = GetComponent(module, "iPhoneScript");
        var fldSolved = GetField<string>(comp, "solved");
        var digits = GetListField<string>(comp, "pinDigits", isPublic: true).Get(expectedLength: 4);

        while (fldSolved.Get() != "solved")
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_iPhone);

        addQuestions(module,
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "first" }, new[] { digits[0] }, new[] { digits[1], digits[2], digits[3] }),
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "second" }, new[] { digits[1] }, new[] { digits[0], digits[2], digits[3] }),
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "third" }, new[] { digits[2] }, new[] { digits[1], digits[0], digits[3] }),
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "fourth" }, new[] { digits[3] }, new[] { digits[1], digits[2], digits[0] }));
    }

    private IEnumerable<object> ProcessJewelVault(KMBombModule module)
    {
        var comp = GetComponent(module, "jewelWheelsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        var wheels = GetArrayField<KMSelectable>(comp, "wheels", isPublic: true).Get(expectedLength: 4);
        var assignedWheels = GetListField<KMSelectable>(comp, "assignedWheels").Get(expectedLength: 4);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_JewelVault);

        addQuestions(module, assignedWheels.Select((aw, ix) => makeQuestion(Question.JewelVaultWheels, _JewelVault, new[] { "ABCD".Substring(ix, 1) }, new[] { (Array.IndexOf(wheels, aw) + 1).ToString() })));
    }

    private IEnumerable<object> ProcessJumbleCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "JumbleCycleScript", Question.JumbleCycleWord, _JumbleCycle);
    }

    private IEnumerable<object> ProcessKudosudoku(KMBombModule module)
    {
        var comp = GetComponent(module, "KudosudokuModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var shown = GetArrayField<bool>(comp, "_shown").Get(expectedLength: 16).ToArray();  // Take a copy of the array because the module changes it

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Kudosudoku);

        addQuestions(module,
            makeQuestion(Question.KudosudokuPrefilled, _Kudosudoku, new[] { "pre-filled" },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: Enumerable.Range(0, 16).Where(ix => shown[ix]).Select(coord => Tiles4x4Sprites[coord]).ToArray()),
            makeQuestion(Question.KudosudokuPrefilled, _Kudosudoku, new[] { "not pre-filled" },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: Enumerable.Range(0, 16).Where(ix => !shown[ix]).Select(coord => Tiles4x4Sprites[coord]).ToArray()));
    }

    private IEnumerable<object> ProcessLasers(KMBombModule module)
    {
        var comp = GetComponent(module, "LasersModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Lasers);

        var laserOrder = GetListField<int>(comp, "_laserOrder").Get(expectedLength: 9);
        var hatchesPressed = GetListField<int>(comp, "_hatchesAlreadyPressed").Get(expectedLength: 7);
        var hatchNames = new[] { "top-left", "top-middle", "top-right", "middle-left", "center", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };
        addQuestions(module, hatchesPressed.Select((hatch, ix) => makeQuestion(Question.LasersHatches, _Lasers, new[] { hatchNames[hatch] }, new[] { laserOrder[hatch].ToString() }, hatchesPressed.Select(number => laserOrder[number].ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessLEDEncryption(KMBombModule module)
    {
        var comp = GetComponent(module, "LEDEncryption");

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var buttons = GetArrayField<KMSelectable>(comp, "buttons", true).Get(expectedLength: 4);
        var buttonLabels = buttons.Select(btn => btn.GetComponentInChildren<TextMesh>()).ToArray();
        if (buttonLabels.Any(x => x == null))
            throw new AbandonModuleException("At least one of the buttons’ TextMesh is null.");

        var multipliers = GetArrayField<int>(comp, "layerMultipliers").Get(minLength: 2, maxLength: 5, validator: m => m < 2 || m > 7 ? "expected range 2–7" : null);
        var numStages = multipliers.Length;
        var pressedLetters = new string[numStages];
        var wrongLetters = new HashSet<string>();
        var fldStage = GetIntField(comp, "layer");

        var reassignButton = Ut.Lambda((KMSelectable btn, TextMesh lbl) =>
        {
            var prevInteract = btn.OnInteract;
            var stage = fldStage.Get();
            btn.OnInteract = delegate
            {
                var label = lbl.text;
                var ret = prevInteract();
                if (fldStage.Get() > stage)
                    pressedLetters[stage] = label;
                return ret;
            };
        });

        while (fldStage.Get() < numStages)
        {
            foreach (var lbl in buttonLabels)
                wrongLetters.Add(lbl.text);

            // LED Encryption re-hooks the buttons at every press, so we have to re-hook it at each stage as well
            for (int i = 0; i < 4; i++)
                reassignButton(buttons[i], buttonLabels[i]);

            var stage = fldStage.Get();
            while (fldStage.Get() == stage)
                yield return null;
        }

        _modulesSolved.IncSafe(_LEDEncryption);
        addQuestions(module, Enumerable.Range(0, pressedLetters.Length - 1)
            .Where(i => pressedLetters[i] != null)
            .Select(stage => makeQuestion(Question.LEDEncryptionPressedLetters, _LEDEncryption, new[] { ordinal(stage + 1) }, new[] { pressedLetters[stage] }, wrongLetters.ToArray())));
    }

    private IEnumerable<object> ProcessLEDMath(KMBombModule module)
    {
        var comp = GetComponent(module, "LEDMathScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var ledA = GetIntField(comp, "ledAIndex").Get(min: 0, max: 3);
        var ledB = GetIntField(comp, "ledBIndex").Get(min: 0, max: 3);
        var ledOp = GetIntField(comp, "ledOpIndex").Get(min: 0, max: 3);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_LEDMath);

        var ledColors = new[] { "Red", "Blue", "Green", "Yellow" };

        addQuestions(module,
            makeQuestion(Question.LEDMathLights, _LEDMath, new[] { "LED A" }, new[] { ledColors[ledA] }),
            makeQuestion(Question.LEDMathLights, _LEDMath, new[] { "LED B" }, new[] { ledColors[ledB] }),
            makeQuestion(Question.LEDMathLights, _LEDMath, new[] { "the operator LED" }, new[] { ledColors[ledOp] }));
    }

    private IEnumerable<object> ProcessLEGOs(KMBombModule module)
    {
        var comp = GetComponent(module, "LEGOModule");

        var solutionStruct = GetField<object>(comp, "SolutionStructure").Get();
        var pieces = GetField<IList>(solutionStruct, "Pieces", isPublic: true).Get(ar => ar.Count != 6 ? "expected length 6" : null);

        // Hook into the module’s OnPass handler
        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        yield return new WaitUntil(() => isSolved);

        // Block the left/right buttons so the player can’t see the instruction pages anymore
        var leftButton = GetField<KMSelectable>(comp, "LeftButton", isPublic: true).Get();
        var rightButton = GetField<KMSelectable>(comp, "RightButton", isPublic: true).Get();

        leftButton.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, module.transform);
            leftButton.AddInteractionPunch(0.5f);
            return false;
        };
        rightButton.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, module.transform);
            rightButton.AddInteractionPunch(0.5f);
            return false;
        };

        // Erase the solution so the player can’t see brick sizes on it either
        var submission = GetArrayField<int>(comp, "Submission").Get();
        for (int i = 0; i < submission.Length; i++)
            submission[i] = 0;
        GetMethod(comp, "UpdateDisplays", numParameters: 0).Invoke();

        // Obtain the brick sizes and colors
        var fldBrickColors = GetIntField(pieces[0], "BrickColor", isPublic: true);
        var fldBrickDimensions = GetArrayField<int>(pieces[0], "Dimensions", isPublic: true);
        var brickColors = Enumerable.Range(0, 6).Select(i => fldBrickColors.GetFrom(pieces[i])).ToList();
        var brickDimensions = Enumerable.Range(0, 6).Select(i => fldBrickDimensions.GetFrom(pieces[i])).ToList();

        _modulesSolved.IncSafe(_LEGOs);
        var colorNames = new[] { "red", "green", "blue", "cyan", "magenta", "yellow" };
        addQuestions(module, Enumerable.Range(0, 6).Select(i => makeQuestion(Question.LEGOsPieceDimensions, _LEGOs, new[] { colorNames[brickColors[i]] }, new[] { brickDimensions[i][0] + "×" + brickDimensions[i][1] })));
    }

    private IEnumerable<object> ProcessListening(KMBombModule module)
    {
        var comp = GetComponent(module, "Listening");
        var fldIsActivated = GetField<bool>(comp, "isActivated");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var attr = _attributes.Get(Question.Listening);
        if (attr == null)
            throw new AbandonModuleException("Abandoning Listening because SouvenirQuestionAttribute for Question.Listening is null.");

        var sound = GetField<object>(comp, "sound").Get();
        var buttons = new[] { "DollarButton", "PoundButton", "StarButton", "AmpersandButton" }.Select(fieldName => GetField<KMSelectable>(comp, fieldName, isPublic: true).Get()).ToArray();

        var prevInteracts = buttons.Select(btn => btn.OnInteract).ToArray();
        var nullIndex = Array.IndexOf(prevInteracts, null);
        if (nullIndex != -1)
            throw new AbandonModuleException("Abandoning Listening because buttons[{0}].OnInteract is null.", nullIndex);

        var correctCode = GetField<string>(sound, "code", isPublic: true).Get();

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
        addQuestion(module, Question.Listening, correctAnswers: new[] { correctCode });
    }

    private IEnumerable<object> ProcessLogicalButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "LogicalButtonsScript");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldStage = GetIntField(comp, "stage");
        var fldButtons = GetField<Array>(comp, "buttons");
        var fldGateOperator = GetField<object>(comp, "gateOperator");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var curStage = 0;
        var colors = new string[3][];
        var labels = new string[3][];
        var initialOperators = new string[3];

        FieldInfo<string> fldLabel = null;
        FieldInfo<object> fldColor = null;
        FieldInfo<int> fldIndex = null;
        MethodInfo<string> mthGetName = null;

        while (!fldSolved.Get())
        {
            var buttons = fldButtons.Get(ar => ar.Length != 3 ? "expected length 3" : null);
            var infs = buttons.Cast<object>().Select(obj =>
            {
                fldLabel = fldLabel ?? GetField<string>(obj, "<Label>k__BackingField");
                fldColor = fldColor ?? GetField<object>(obj, "<Color>k__BackingField");
                fldIndex = fldIndex ?? GetIntField(obj, "<Index>k__BackingField");
                return fldLabel == null || fldColor == null || fldIndex == null
                    ? null
                    : new { Label = fldLabel.GetFrom(obj), Color = fldColor.GetFrom(obj), Index = fldIndex.GetFrom(obj) };
            }).ToArray();
            if (infs.Length != 3 || infs.Any(inf => inf == null || inf.Label == null || inf.Color == null) || infs[0].Index != 0 || infs[1].Index != 1 || infs[2].Index != 2)
                throw new AbandonModuleException("I got an unexpected value ([{0}]).", infs.Select(inf => inf == null ? "<null>" : inf.ToString()).JoinString(", "));

            var gateOperator = fldGateOperator.Get();
            if (mthGetName == null)
            {
                var interfaceType = gateOperator.GetType().Assembly.GetType("ILogicalGateOperator");
                if (interfaceType == null)
                    throw new AbandonModuleException("Interface type ILogicalGateOperator not found.");

                var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                var mths = interfaceType.GetMethods(bindingFlags).Where(m => m.Name == "get_Name" && m.GetParameters().Length == 0 && typeof(string).IsAssignableFrom(m.ReturnType)).Take(2).ToArray();
                if (mths.Length == 0)
                    throw new AbandonModuleException("Type {0} does not contain {1} method {2} with return type {3} and {4} parameters.", interfaceType, "public", name, "string", 0);
                if (mths.Length > 1)
                    throw new AbandonModuleException("Type {0} contains multiple {1} methods {2} with return type {3} and {4} parameters.", interfaceType, "public", name, "string", 0);
                mthGetName = new MethodInfo<string>(null, mths[0]);
            }

            var clrs = infs.Select(inf => inf.Color.ToString()).ToArray();
            var lbls = infs.Select(inf => inf.Label).ToArray();
            var iOp = mthGetName.InvokeOn(gateOperator);

            var stage = fldStage.Get();
            if (stage != curStage || !clrs.SequenceEqual(colors[stage - 1]) || !lbls.SequenceEqual(labels[stage - 1]) || iOp != initialOperators[stage - 1])
            {
                if (stage != curStage && stage != curStage + 1)
                    throw new AbandonModuleException("I must have missed a stage (it went from {0} to {1}).", curStage, stage);
                if (stage < 1 || stage > 3)
                    throw new AbandonModuleException("‘stage’ has unexpected value {0} (expected 1–3).", stage);

                colors[stage - 1] = clrs;
                labels[stage - 1] = lbls;
                initialOperators[stage - 1] = iOp;
                curStage = stage;
            }

            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_LogicalButtons);
        if (initialOperators.Any(io => io == null))
            throw new AbandonModuleException("There is a null initial operator ([{0}]).", initialOperators.Select(io => io == null ? "<null>" : string.Format(@"""{0}""", io)).JoinString(", "));

        var _logicalButtonsButtonNames = new[] { "top", "bottom-left", "bottom-right" };
        addQuestions(module,
            colors.SelectMany((clrs, stage) => clrs.Select((clr, btnIx) => makeQuestion(Question.LogicalButtonsColor, _LogicalButtons, new[] { _logicalButtonsButtonNames[btnIx], ordinal(stage + 1) }, new[] { clr })))
                .Concat(labels.SelectMany((lbls, stage) => lbls.Select((lbl, btnIx) => makeQuestion(Question.LogicalButtonsLabel, _LogicalButtons, new[] { _logicalButtonsButtonNames[btnIx], ordinal(stage + 1) }, new[] { lbl }))))
                .Concat(initialOperators.Select((op, stage) => makeQuestion(Question.LogicalButtonsOperator, _LogicalButtons, new[] { ordinal(stage + 1) }, new[] { op }))));
    }

    private IEnumerable<object> ProcessLogicGates(KMBombModule module)
    {
        var comp = GetComponent(module, "LogicGates");
        var gates = GetField<IList>(comp, "_gates").Get(lst => lst.Count == 0 ? "empty" : null);
        var btnNext = GetField<KMSelectable>(comp, "ButtonNext", isPublic: true).Get();
        var btnPrevious = GetField<KMSelectable>(comp, "ButtonPrevious", isPublic: true).Get();
        var tmpGateType = GetField<object>(gates[0], "GateType", isPublic: true).Get();
        var fldGateTypeName = GetField<string>(tmpGateType, "Name", isPublic: true);

        var gateTypeNames = gates.Cast<object>().Select(obj => fldGateTypeName.GetFrom(GetField<object>(gates[0], "GateType", isPublic: true).GetFrom(obj)).ToString()).ToArray();
        string duplicate = null;
        bool isDuplicateInvalid = false;
        for (int i = 0; i < gateTypeNames.Length; i++)
            for (int j = i + 1; j < gateTypeNames.Length; j++)
                if (gateTypeNames[i] == gateTypeNames[j])
                {
                    if (duplicate != null)
                        isDuplicateInvalid = true;
                    else
                        duplicate = gateTypeNames[i];
                }

        // Unfortunately Logic Gates has no “isSolved” field, so we need to hook into the module
        var solved = false;
        module.OnPass += delegate { solved = true; return true; };

        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_LogicGates);

        btnNext.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btnNext.transform);
            btnNext.AddInteractionPunch(0.2f);
            return false;
        };
        btnPrevious.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btnPrevious.transform);
            btnPrevious.AddInteractionPunch(0.2f);
            return false;
        };

        var qs = new List<QandA>();
        for (int i = 0; i < gateTypeNames.Length; i++)
            qs.Add(makeQuestion(Question.LogicGatesGates, _LogicGates, new[] { "gate " + (char) ('A' + i) }, new[] { gateTypeNames[i] }));
        if (!isDuplicateInvalid)
            qs.Add(makeQuestion(Question.LogicGatesGates, _LogicGates, new[] { "the duplicated gate" }, new[] { duplicate }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessLondonUnderground(KMBombModule module)
    {
        var comp = GetComponent(module, "londonUndergroundScript");
        var fldStage = GetIntField(comp, "levelsPassed");
        var fldDepartureStation = GetField<string>(comp, "departureStation");
        var fldDestinationStation = GetField<string>(comp, "destinationStation");
        var fldDepartureOptions = GetArrayField<string>(comp, "departureOptions");
        var fldDestinationOptions = GetArrayField<string>(comp, "destinationOptions");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        var departures = new List<string>();
        var destinations = new List<string>();
        var extraOptions = new HashSet<string>();
        var lastStage = -1;
        while (!fldSolved.Get())
        {
            var stage = fldStage.Get();
            if (stage != lastStage)
            {
                if (stage == 0)
                {
                    // The player got a strike and the module reset
                    departures.Clear();
                    destinations.Clear();
                    extraOptions.Clear();
                }
                departures.Add(fldDepartureStation.Get());
                destinations.Add(fldDestinationStation.Get());

                foreach (var option in fldDepartureOptions.Get())
                    extraOptions.Add(option);
                foreach (var option in fldDestinationOptions.Get())
                    extraOptions.Add(option);
                lastStage = stage;
            }
            yield return null;
        }
        _modulesSolved.IncSafe(_LondonUnderground);
        var primary = departures.Union(destinations).ToArray();
        if (primary.Length < 4)
            primary = primary.Union(extraOptions).ToArray();

        addQuestions(module,
            departures.Select((dep, ix) => makeQuestion(Question.LondonUndergroundStations, _LondonUnderground, new[] { ordinal(ix + 1), "depart from" }, new[] { dep }, primary)).Concat(
            destinations.Select((dest, ix) => makeQuestion(Question.LondonUndergroundStations, _LondonUnderground, new[] { ordinal(ix + 1), "arrive to" }, new[] { dest }, primary))));
    }

    private IEnumerable<object> ProcessMafia(KMBombModule module)
    {
        var comp = GetComponent(module, "MafiaModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Mafia);

        var godfather = GetField<object>(comp, "_godfather").Get();
        var suspects = GetField<Array>(comp, "_suspects").Get(ar => ar.Length != 8 ? "expected length 8" : null);
        addQuestion(module, Question.MafiaPlayers, correctAnswers: suspects.Cast<object>().Select(obj => obj.ToString()).Except(new[] { godfather.ToString() }).ToArray());
    }

    private IEnumerable<object> ProcessMahjong(KMBombModule module)
    {
        var comp = GetComponent(module, "MahjongModule");

        // Capture the player’s matching pairs until the module is solved
        var taken = GetArrayField<bool>(comp, "_taken").Get();
        var currentTaken = taken.ToArray();
        var matchedTiles = new List<int>();

        while (true)
        {
            yield return null;
            if (!currentTaken.SequenceEqual(taken))
            {
                matchedTiles.AddRange(Enumerable.Range(0, taken.Length).Where(ix => currentTaken[ix] != taken[ix]));
                if (taken.All(x => x))
                    break;
                currentTaken = taken.ToArray();
            }
        }
        _modulesSolved.IncSafe(_Mahjong);

        // Remove the counting tile, complete with smoke animation
        var countingTile = GetField<MeshRenderer>(comp, "CountingTile", true).Get();
        if (countingTile.gameObject.activeSelf)     // Do it only if another Souvenir module on the same bomb hasn’t already done it
        {
            var smoke = GetField<ParticleSystem>(comp, "Smoke1", true).Get();
            GetField<KMAudio>(comp, "Audio", true).Get().PlaySoundAtTransform("Elimination", countingTile.transform);
            smoke.transform.localPosition = countingTile.transform.localPosition;
            smoke.Play();
            countingTile.gameObject.SetActive(false);
        }

        // Stuff for the “counting tile” question (bottom-left of the module)
        var countingTileName = countingTile.material.mainTexture.name.Replace(" normal", "");
        var countingTileSprite = MahjongSprites.FirstOrDefault(x => x.name == countingTileName);
        if (countingTileSprite == null)
            throw new AbandonModuleException("The sprite for the counting tile ({0}) doesn’t exist.", countingTileName);

        // Stuff for the “matching tiles” question
        var matchRow1 = GetArrayField<int>(comp, "_matchRow1").Get();
        var matchRow2 = GetArrayField<int>(comp, "_matchRow2").Get();
        var tileSelectables = GetArrayField<KMSelectable>(comp, "Tiles", true).Get();

        var tileSprites = matchRow1.Concat(matchRow2).Select(ix => MahjongSprites[ix]).ToArray();
        var matchedTileSpriteNames = matchedTiles.Select(ix => tileSelectables[ix].GetComponent<MeshRenderer>().material.mainTexture.name.Replace(" normal", "").Replace(" highlighted", "")).ToArray();
        var matchedTileSprites = matchedTileSpriteNames.Select(name => tileSprites.FirstOrDefault(spr => spr.name == name)).ToArray();

        var invalidIx = matchedTileSprites.IndexOf(spr => spr == null);
        if (invalidIx != -1)
            throw new AbandonModuleException("The sprite for one of the matched tiles ({0}) doesn’t exist. matchedTileSpriteNames=[{1}], matchedTileSprites=[{2}], countingRow=[{3}], matchRow1=[{4}], matchRow2=[{5}], tileSprites=[{6}]",
                matchedTileSpriteNames[invalidIx], matchedTileSpriteNames.JoinString(", "), matchedTileSprites.Select(spr => spr == null ? "<null>" : spr.name).JoinString(", "),
                GetArrayField<int>(comp, "_countingRow").Get().JoinString(", "), matchRow1.JoinString(", "), matchRow2.JoinString(", "), tileSprites.Select(spr => spr.name).JoinString(", "));

        addQuestions(module,
            makeQuestion(Question.MahjongCountingTile, _Mahjong, correctAnswers: new[] { countingTileSprite }, preferredWrongAnswers: GetArrayField<int>(comp, "_countingRow").Get().Select(ix => MahjongSprites[ix]).ToArray()),
            makeQuestion(Question.MahjongMatches, _Mahjong, new[] { "first" }, correctAnswers: matchedTileSprites.Take(2).ToArray(), preferredWrongAnswers: tileSprites),
            makeQuestion(Question.MahjongMatches, _Mahjong, new[] { "second" }, correctAnswers: matchedTileSprites.Skip(2).Take(2).ToArray(), preferredWrongAnswers: tileSprites));
    }

    private IEnumerable<object> ProcessMaritimeFlags(KMBombModule module)
    {
        var comp = GetComponent(module, "MaritimeFlagsModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MaritimeFlags);

        var bearing = GetIntField(comp, "_bearingOnModule").Get(min: 0, max: 359);
        var callsignObj = GetField<object>(comp, "_callsign").Get();
        var callsign = GetField<string>(callsignObj, "Name", isPublic: true).Get(str => str.Length != 7 ? "expected length 7" : null);

        addQuestions(module,
            makeQuestion(Question.MaritimeFlagsBearing, _MaritimeFlags, correctAnswers: new[] { bearing.ToString() }),
            makeQuestion(Question.MaritimeFlagsCallsign, _MaritimeFlags, correctAnswers: new[] { callsign.ToLowerInvariant() }));
    }

    private IEnumerable<object> ProcessMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "InvisibleWallsComponent");
        var fldSolved = GetField<bool>(comp, "IsSolved", true);

        var currentCell = GetProperty<object>(comp, "CurrentCell", isPublic: true).Get();  // Need to get the current cell at the start.
        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Maze);

        addQuestions(module,
            makeQuestion(Question.MazeStartingPosition, _Maze, formatArgs: new[] { "column", "left" }, correctAnswers: new[] { (GetIntField(currentCell, "X", true).Get() + 1).ToString() }),
            makeQuestion(Question.MazeStartingPosition, _Maze, formatArgs: new[] { "row", "top" }, correctAnswers: new[] { (GetIntField(currentCell, "Y", true).Get() + 1).ToString() }));
    }

    private IEnumerable<object> ProcessMaze3(KMBombModule module)
    {
        var comp = GetComponent(module, "maze3Script");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var node = GetIntField(comp, "node").Get(min: 0, max: 53);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Maze3);

        var colors = new[] { "Red", "Blue", "Yellow", "Green", "Magenta", "Orange" };
        addQuestion(module, Question.Maze3StartingFace, correctAnswers: new[] { colors[node / 9] });
    }

    private IEnumerable<object> ProcessMazematics(KMBombModule module)
    {
        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Mazematics);

        var comp = GetComponent(module, "Mazematics");
        var startVal = GetIntField(comp, "startValue").Get(17, 49).ToString();
        var goalVal = GetIntField(comp, "goalValue").Get(0, 49).ToString();

        string[] possibleStartVals = Enumerable.Range(17, 33).Select(x => x.ToString()).ToArray();
        string[] possibleGoalVals = Enumerable.Range(0, 50).Select(x => x.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.MazematicsValue, _Mazematics, new[] { "initial" }, new[] { startVal }, possibleStartVals),
            makeQuestion(Question.MazematicsValue, _Mazematics, new[] { "goal" }, new[] { goalVal }, possibleGoalVals));
    }

    private IEnumerable<object> ProcessMazeScrambler(KMBombModule module)
    {
        var comp = GetComponent(module, "MazeScrambler");
        var fldSolved = GetField<bool>(comp, "SOLVED");

        var ind1X = GetIntField(comp, "IDX1").Get(min: 0, max: 2);
        var ind1Y = GetIntField(comp, "IDY1").Get(min: 0, max: 2);
        var ind2X = GetIntField(comp, "IDX2").Get(min: 0, max: 2);
        var ind2Y = GetIntField(comp, "IDY2").Get(min: 0, max: 2);
        var startX = GetIntField(comp, "StartX").Get(min: 0, max: 2);
        var startY = GetIntField(comp, "StartY").Get(min: 0, max: 2);
        var goalX = GetIntField(comp, "GoalX").Get(min: 0, max: 2);
        var goalY = GetIntField(comp, "GoalY").Get(min: 0, max: 2);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        var positionNames = new[] { "top-left", "top-middle", "top-right", "middle-left", "center", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };

        _modulesSolved.IncSafe(_MazeScrambler);
        addQuestions(module,
            makeQuestion(Question.MazeScramblerStart, _MazeScrambler, correctAnswers: new[] { positionNames[startY * 3 + startX] }, preferredWrongAnswers: new[] { positionNames[goalY * 3 + goalX] }),
            makeQuestion(Question.MazeScramblerGoal, _MazeScrambler, correctAnswers: new[] { positionNames[goalY * 3 + goalX] }, preferredWrongAnswers: new[] { positionNames[startY * 3 + startX] }),
            makeQuestion(Question.MazeScramblerIndicators, _MazeScrambler, correctAnswers: new[] { positionNames[ind1Y * 3 + ind1X], positionNames[ind2Y * 3 + ind2X] }, preferredWrongAnswers: positionNames));
    }

    private IEnumerable<object> ProcessMegaMan2(KMBombModule module)
    {
        var comp = GetComponent(module, "Megaman2");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var robotMasters = GetArrayField<string>(comp, "robotMasters").Get();
        var selectedMaster = GetIntField(comp, "selectedMaster").Get(min: 0, max: robotMasters.Length - 1);
        var selectedWeapon = GetIntField(comp, "selectedWeapon").Get(min: 0, max: robotMasters.Length - 1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MegaMan2);

        addQuestions(module,
            makeQuestion(Question.MegaMan2SelectedMaster, _MegaMan2, correctAnswers: new[] { robotMasters[selectedMaster] }, preferredWrongAnswers: robotMasters),
            makeQuestion(Question.MegaMan2SelectedWeapon, _MegaMan2, correctAnswers: new[] { robotMasters[selectedWeapon] }, preferredWrongAnswers: robotMasters));
    }

    private IEnumerable<object> ProcessMelodySequencer(KMBombModule module)
    {
        var comp = GetComponent(module, "MelodySequencerScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        var parts = GetArrayField<int[]>(comp, "parts").Get(expectedLength: 8);  // the 8 parts in their “correct” order
        var moduleParts = GetArrayField<int[]>(comp, "moduleParts").Get(expectedLength: 8);      // the parts as assigned to the slots
        var partsPerSlot = Enumerable.Range(0, 8).Select(slot => parts.IndexOf(p => ReferenceEquals(p, moduleParts[slot]))).ToArray();
        Debug.LogFormat("<Souvenir #{0}> Melody Sequencer: parts are: [{1}].", _moduleId, partsPerSlot.JoinString(", "));

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MelodySequencer);

        var qs = new List<QandA>();
        var givenSlots = Enumerable.Range(0, partsPerSlot.Length).Where(slot => partsPerSlot[slot] != -1).Select(slot => (slot + 1).ToString()).ToArray();
        var givenParts = partsPerSlot.Where(part => part != -1).Select(part => (part + 1).ToString()).ToArray();
        for (int i = 0; i < partsPerSlot.Length; i++)
        {
            if (partsPerSlot[i] != -1)
            {
                qs.Add(makeQuestion(Question.MelodySequencerParts, _MelodySequencer, new[] { (partsPerSlot[i] + 1).ToString() }, new[] { (i + 1).ToString() }, preferredWrongAnswers: givenSlots));
                qs.Add(makeQuestion(Question.MelodySequencerSlots, _MelodySequencer, new[] { (i + 1).ToString() }, new[] { (partsPerSlot[i] + 1).ToString() }, preferredWrongAnswers: givenParts));
            }
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessMemorableButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "MemorableButtons");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var buttonLabels = GetArrayField<TextMesh>(comp, "buttonLabels", isPublic: true).Get(ar => ar.Length == 0 ? "empty" : null);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MemorableButtons);

        var combinedCode = GetField<string>(comp, "combinedCode", isPublic: true).Get(str => str.Length < 10 || str.Length > 15 ? "expected length 10–15" : null);
        addQuestions(module, combinedCode.Select((ch, ix) => makeQuestion(Question.MemorableButtonsSymbols, _MemorableButtons, buttonLabels[0].font, buttonLabels[0].GetComponent<MeshRenderer>().sharedMaterial.mainTexture, new[] { ordinal(ix + 1) }, correctAnswers: new[] { ch.ToString() })));
    }

    private IEnumerable<object> ProcessMemory(KMBombModule module)
    {
        var comp = GetComponent(module, "MemoryComponent");
        var fldSolved = GetField<bool>(comp, "IsSolved", true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Memory);

        var displaySequence = GetProperty<string>(comp, "DisplaySequence", true).Get();
        var indices = GetListField<int>(comp, "buttonIndicesPressed", false).Get();
        var labels = GetListField<string>(comp, "buttonLabelsPressed", false).Get();

        var stage = Rnd.Range(0, 4);
        addQuestions(module,
            makeQuestion(Question.MemoryDisplay, "Memory", new[] { (stage + 1).ToString() }, new[] { displaySequence[stage].ToString() }),
            makeQuestion(Question.MemoryPosition, "Memory", new[] { (stage + 1).ToString() }, new[] { MemorySprites[indices[stage]] }, MemorySprites),
            makeQuestion(Question.MemoryLabel, "Memory", new[] { (stage + 1).ToString() }, new[] { labels[stage][labels[stage].Length - 1].ToString() }));
    }

    private IEnumerable<object> ProcessMicrocontroller(KMBombModule module)
    {
        var comp = GetComponent(module, "Micro");
        var fldSolved = GetIntField(comp, "solved");

        while (fldSolved.Get() == 0)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Microcontroller);

        var ledsOrder = GetListField<int>(comp, "LEDorder").Get(lst => lst.Count != 6 && lst.Count != 8 && lst.Count != 10 ? "unexpected length (expected 6, 8 or 10)" : null);
        var positionTranslate = GetArrayField<int>(comp, "positionTranslate").Get(expectedLength: ledsOrder.Count);

        addQuestions(module, ledsOrder.Select((led, ix) => makeQuestion(Question.MicrocontrollerPinOrder, _Microcontroller,
            formatArgs: new[] { ordinal(ix + 1) },
            correctAnswers: new[] { (positionTranslate[led] + 1).ToString() },
            preferredWrongAnswers: Enumerable.Range(1, ledsOrder.Count).Select(i => i.ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessMinesweeper(KMBombModule module)
    {
        var comp = GetComponent(module, "MinesweeperModule");

        // Wait for activation as the above fields aren’t fully initialized until then
        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var propSolved = GetProperty<bool>(GetField<object>(comp, "Game").Get(), "Solved", isPublic: true);
        var color = GetField<string>(GetField<object>(comp, "StartingCell").Get(), "Color", isPublic: true).Get();

        while (!propSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_Minesweeper);
        addQuestion(module, Question.MinesweeperStartingColor, correctAnswers: new[] { color });
    }

    private IEnumerable<object> ProcessModernCipher(KMBombModule module)
    {
        var comp = GetComponent(module, "modernCipher");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        var dictionary = GetField<Dictionary<string, string>>(comp, "chosenWords").Get();

        string stage1word, stage2word;
        if (!dictionary.TryGetValue("Stage1", out stage1word) || !dictionary.TryGetValue("Stage2", out stage2word) || stage1word == null || stage2word == null)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Modern Cipher because there is no word for {1}.", _moduleId, stage1word == null ? "stage 1" : "stage 2");
            yield break;
        }

        Debug.LogFormat("<Souvenir #{0}> Modern Cipher words: {1} {2}.", _moduleId, stage1word, stage2word);

        stage1word = stage1word.Substring(0, 1).ToUpperInvariant() + stage1word.Substring(1).ToLowerInvariant();
        stage2word = stage2word.Substring(0, 1).ToUpperInvariant() + stage2word.Substring(1).ToLowerInvariant();

        _modulesSolved.IncSafe(_ModernCipher);
        addQuestions(module,
            makeQuestion(Question.ModernCipherWord, _ModernCipher, new[] { "first" }, new[] { stage1word }, new[] { stage2word }),
            makeQuestion(Question.ModernCipherWord, _ModernCipher, new[] { "second" }, new[] { stage2word }, new[] { stage1word }));
    }

    private IEnumerable<object> ProcessModuleMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "ModuleMazeModule");
        var fldSprites = GetArrayField<Sprite>(comp, "souvenirSprites", true);

        while (fldSprites.Get().Count() < 6)
            yield return new WaitForSeconds(.1f);

        var sprites = fldSprites.Get();
        var start = GetField<string>(comp, "souvenirStart", true).Get();

        _modulesSolved.IncSafe(_ModuleMaze);

        addQuestions(module,
            makeQuestion(Question.ModuleMazeStartingIcon, _ModuleMaze,
                correctAnswers: new[] { sprites.FirstOrDefault(spr => spr.name == start) }, preferredWrongAnswers: sprites));
    }

    private IEnumerable<object> ProcessMonsplodeFight(KMBombModule module)
    {
        var comp = GetComponent(module, "MonsplodeFightModule");
        var fldCreatureID = GetIntField(comp, "crID");
        var fldMoveIDs = GetArrayField<int>(comp, "moveIDs");
        var fldRevive = GetField<bool>(comp, "revive");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var creatureData = GetField<object>(comp, "CD", isPublic: true).Get();
        var movesData = GetField<object>(comp, "MD", isPublic: true).Get();
        var buttons = GetArrayField<KMSelectable>(comp, "buttons", isPublic: true).Get(expectedLength: 4);
        var creatureNames = GetArrayField<string>(creatureData, "names", isPublic: true).Get();
        var moveNames = GetArrayField<string>(movesData, "names", isPublic: true).Get();

        string displayedCreature = null;
        string[] displayedMoves = null;
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
                        Debug.LogFormat("<Souvenir #{2}> Monsplode, Fight!: Unexpected creature ID: {0}; creature names are: [{1}]", creatureID, creatureNames.Select(cn => cn == null ? "null" : '"' + cn + '"').JoinString(", "), _moduleId);
                    else
                    {
                        // Make sure not to throw exceptions inside of the module’s button handler!
                        var moveIDs = fldMoveIDs.Get(nullAllowed: true);
                        if (moveIDs == null || moveIDs.Length != 4 || moveIDs.Any(mid => mid >= moveNames.Length || string.IsNullOrEmpty(moveNames[mid])))
                            Debug.LogFormat("<Souvenir #{2}> Monsplode, Fight!: Unexpected move IDs: {0}; moves names are: [{1}]",
                                moveIDs == null ? null : "[" + moveIDs.JoinString(", ") + "]",
                                moveNames.Select(mn => mn == null ? "null" : '"' + mn + '"').JoinString(", "),
                                _moduleId);
                        else
                        {
                            curCreatureName = creatureNames[creatureID];
                            curMoveNames = moveIDs.Select(mid => moveNames[mid].Replace("\r", "").Replace("\n", " ")).ToArray();
                        }
                    }

                    var ret = origInteracts[j]();

                    if (curCreatureName == null || curMoveNames == null)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Monsplode, Fight!: Abandoning due to error above.", _moduleId);
                        // Set these to null to signal that something went wrong and we need to abort
                        displayedCreature = null;
                        displayedMoves = null;
                        finished = true;
                    }
                    else
                    {
                        // If ‘revive’ is ‘false’, there is not going to be another stage.
                        if (!fldRevive.Get())
                            finished = true;

                        if (curCreatureName != null && curMoveNames != null)
                        {
                            displayedCreature = curCreatureName;
                            displayedMoves = curMoveNames;
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

        // If either of these is the case, an error message will already have been output above (within the button handler)
        if (displayedCreature == null || displayedMoves == null)
            yield break;

        addQuestions(module,
            makeQuestion(Question.MonsplodeFightCreature, _MonsplodeFight, correctAnswers: new[] { displayedCreature }),
            makeQuestion(Question.MonsplodeFightMove, _MonsplodeFight, new[] { "was" }, displayedMoves),
            makeQuestion(Question.MonsplodeFightMove, _MonsplodeFight, new[] { "was not" }, _attributes.Get(Question.MonsplodeFightMove).AllAnswers.Except(displayedMoves).ToArray()));
    }

    private IEnumerable<object> ProcessMonsplodeTradingCards(KMBombModule module)
    {
        var comp = GetComponent(module, "MonsplodeCardModule");
        var fldStage = GetIntField(comp, "correctOffer", isPublic: true);

        var stageCount = GetIntField(comp, "offerCount", isPublic: true).Get(min: 3, max: 3);
        var data = GetField<object>(comp, "CD", isPublic: true).Get();
        var monsplodeNames = GetArrayField<string>(data, "names", isPublic: true).Get();

        while (fldStage.Get() < stageCount)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MonsplodeTradingCards);

        if (fldStage.Get() != stageCount)
            throw new AbandonModuleException("Abandoning Monsplode Trading Cards because ‘correctOffer’ has unexpected value {0} instead of {1}.", fldStage.Get(), stageCount);

        var deck = GetField<Array>(comp, "deck", isPublic: true).Get(ar => ar.Length != 3 ? "expected length 3" : null).Cast<object>().ToArray();
        var offer = GetField<object>(comp, "offer", isPublic: true).Get();
        var fldMonsplode = GetIntField(offer, "monsplode", isPublic: true);
        var fldRarity = GetIntField(offer, "rarity", isPublic: true);
        var fldPrintDigit = GetIntField(offer, "printDigit", isPublic: true);
        var fldPrintChar = GetField<char>(offer, "printChar", isPublic: true);

        var monsplodeIds = new[] { fldMonsplode.Get(0, monsplodeNames.Length - 1) }.Concat(deck.Select(card => fldMonsplode.GetFrom(card, 0, monsplodeNames.Length - 1))).ToArray();
        var monsplodes = monsplodeIds.Select(mn => monsplodeNames[mn].Replace("\r", "").Replace("\n", " ")).ToArray();
        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "card on offer" }, new[] { monsplodes[0] }, monsplodeNames));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "first card in your hand" }, new[] { monsplodes[1] }, monsplodeNames));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "second card in your hand" }, new[] { monsplodes[2] }, monsplodeNames));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "third card in your hand" }, new[] { monsplodes[3] }, monsplodeNames));

        var rarityNames = new[] { "common", "uncommon", "rare", "ultra rare" };
        var rarityIds = new[] { fldRarity.Get(min: 0, max: rarityNames.Length - 1) }.Concat(deck.Select(card => fldRarity.GetFrom(card, min: 0, max: rarityNames.Length - 1))).ToArray();
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "card on offer" }, new[] { rarityNames[rarityIds[0]] }));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "first card in your hand" }, new[] { rarityNames[rarityIds[1]] }));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "second card in your hand" }, new[] { rarityNames[rarityIds[2]] }));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "third card in your hand" }, new[] { rarityNames[rarityIds[3]] }));

        var printVersions = new[] { fldPrintChar.Get() + "" + fldPrintDigit.Get() }.Concat(deck.Select(card => fldPrintChar.GetFrom(card) + "" + fldPrintDigit.GetFrom(card))).ToArray();
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "card on offer" }, new[] { printVersions[0] }, printVersions));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "first card in your hand" }, new[] { printVersions[1] }, printVersions));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "second card in your hand" }, new[] { printVersions[2] }, printVersions));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "third card in your hand" }, new[] { printVersions[3] }, printVersions));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessMoon(KMBombModule module)
    {
        var comp = GetComponent(module, "theMoonScript");
        var fldStage = GetIntField(comp, "stage");

        // The Moon sets ‘stage’ to 9 when the module is solved.
        while (fldStage.Get() != 9)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Moon);

        var lightIndex = GetIntField(comp, "lightIndex").Get(min: 0, max: 7);
        var qNames = new[] { "first initially lit", "second initially lit", "third initially lit", "fourth initially lit", "first initially unlit", "second initially unlit", "third initially unlit", "fourth initially unlit" };
        var aNames = new[] { "south", "south-west", "west", "north-west", "north", "north-east", "east", "south-east" };
        addQuestions(module, Enumerable.Range(0, 8).Select(i => makeQuestion(Question.MoonLitUnlit, _Moon, new[] { qNames[i] }, new[] { aNames[(i + lightIndex) % 8] })));
    }

    private IEnumerable<object> ProcessMorseAMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "MorseAMaze");

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var start = GetField<string>(comp, "_souvenirQuestionStartingLocation").Get(str => str.Length != 2 ? "expected length 2" : null);
        var end = GetField<string>(comp, "_souvenirQuestionEndingLocation").Get(str => str.Length != 2 ? "expected length 2" : null);
        var word = GetField<string>(comp, "_souvenirQuestionWordPlaying").Get(str => str.Length < 4 ? "expected length ≥ 4" : null);
        var words = GetArrayField<string>(comp, "_souvenirQuestionWordList").Get(expectedLength: 36);

        var fldSolved = GetField<bool>(comp, "_solved");
        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_MorseAMaze);
        addQuestions(module,
            makeQuestion(Question.MorseAMazeStartingCoordinate, _MorseAMaze, correctAnswers: new[] { start }),
            makeQuestion(Question.MorseAMazeEndingCoordinate, _MorseAMaze, correctAnswers: new[] { end }),
            makeQuestion(Question.MorseAMazeMorseCodeWord, _MorseAMaze, correctAnswers: new[] { word }, preferredWrongAnswers: words));
    }

    private IEnumerable<object> ProcessMorseButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "morseButtonsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        string alphabet = GetField<string>(comp, "alphabet").Get();
        var colorNames = new[] { "Red", "Blue", "Green", "Yellow", "Orange", "Purple" };
        int[] letters = GetArrayField<int>(comp, "letters").Get(expectedLength: 6, validator: x => x < 0 || x >= alphabet.Length ? "out of range" : null);
        int[] colors = GetArrayField<int>(comp, "colors").Get(expectedLength: 6, validator: x => x < 0 || x >= colorNames.Length ? "out of range" : null);

        _modulesSolved.IncSafe(_MorseButtons);
        addQuestions(module,
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "first" }, new[] { alphabet[letters[0]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "second" }, new[] { alphabet[letters[1]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "third" }, new[] { alphabet[letters[2]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "fourth" }, new[] { alphabet[letters[3]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "fifth" }, new[] { alphabet[letters[4]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "sixth" }, new[] { alphabet[letters[5]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "first" }, new[] { colorNames[colors[0]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "second" }, new[] { colorNames[colors[1]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "third" }, new[] { colorNames[colors[2]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "fourth" }, new[] { colorNames[colors[3]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "fifth" }, new[] { colorNames[colors[4]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "sixth" }, new[] { colorNames[colors[5]].ToString() }, colorNames));
    }

    private IEnumerable<object> ProcessMorsematics(KMBombModule module)
    {
        var comp = GetComponent(module, "AdvancedMorse");
        var fldSolved = GetField<bool>(comp, "solved");
        var chars = GetArrayField<string>(comp, "DisplayCharsRaw").Get(expectedLength: 3);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Morsematics);
        addQuestions(module, Enumerable.Range(0, 3).Select(i => makeQuestion(Question.MorsematicsReceivedLetters, _Morsematics, new[] { ordinal(i + 1) }, new[] { chars[i] }, chars)));
    }

    private IEnumerable<object> ProcessMorseWar(KMBombModule module)
    {
        var comp = GetComponent(module, "MorseWar");
        var fldIsSolved = GetField<bool>(comp, "isSolved");

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MorseWar);

        var wordTable = GetStaticField<string[]>(comp.GetType(), "WordTable").Get();
        var rowTable = GetStaticField<string[]>(comp.GetType(), "RowTable").Get(ar => ar.Length != 6 ? "expected length 6" : null);
        var wordNum = GetIntField(comp, "wordNum").Get(min: 0, max: wordTable.Length - 1);
        var lights = GetField<string>(comp, "lights").Get(str => str.Length != 3 ? "expected length 3" : str.Any(ch => ch < '1' || ch > '6') ? "expected characters 1–6" : null);

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.MorseWarCode, _MorseWar, correctAnswers: new[] { wordTable[wordNum].ToUpperInvariant() }));
        var rowNames = new[] { "bottom", "middle", "top" };
        for (int i = 0; i < 3; i++)
            qs.Add(makeQuestion(Question.MorseWarLeds, _MorseWar, formatArgs: new[] { rowNames[i] }, correctAnswers: new[] { rowTable[lights[i] - '1'] }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessMouseInTheMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "MouseInTheMazeModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var sphereColors = GetArrayField<int>(comp, "sphereColors").Get(expectedLength: 4);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var goalPos = GetIntField(comp, "goalPosition").Get(min: 0, max: 3);
        var torusColor = GetIntField(comp, "torusColor").Get(min: 0, max: 3);
        var goalColor = sphereColors[goalPos];
        if (goalColor < 0 || goalColor > 3)
            throw new AbandonModuleException("Unexpected color (torus={0}; goal={1})", torusColor, goalColor);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_MouseInTheMaze);
        addQuestions(module,
            makeQuestion(Question.MouseInTheMazeSphere, _MouseInTheMaze, correctAnswers: new[] { new[] { "white", "green", "blue", "yellow" }[goalColor] }),
            makeQuestion(Question.MouseInTheMazeTorus, _MouseInTheMaze, correctAnswers: new[] { new[] { "white", "green", "blue", "yellow" }[torusColor] }));
    }

    private IEnumerable<object> ProcessMurder(KMBombModule module)
    {
        var comp = GetComponent(module, "MurderModule");
        var fldSolved = GetField<bool>(comp, "isSolved");

        // Just a consistency check
        GetIntField(comp, "suspects").Get(min: 4, max: 4);
        GetIntField(comp, "weapons").Get(min: 4, max: 4);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Murder);

        var solution = GetArrayField<int>(comp, "solution").Get(expectedLength: 3);
        var skipDisplay = GetField<int[,]>(comp, "skipDisplay").Get(ar => ar.GetLength(0) != 2 || ar.GetLength(1) != 6 ? string.Format("dimensions are {0},{1}; expected 2,6", ar.GetLength(0), ar.GetLength(1)) : null);
        var names = GetField<string[,]>(comp, "names").Get(ar => ar.GetLength(0) != 3 || ar.GetLength(1) != 9 ? string.Format("dimensions are {0},{1}; expected 3,9", ar.GetLength(0), ar.GetLength(1)) : null);
        var actualSuspect = solution[0];
        var actualWeapon = solution[1];
        var actualRoom = solution[2];
        var bodyFound = GetIntField(comp, "bodyFound").Get();
        if (actualSuspect < 0 || actualSuspect >= 6 || actualWeapon < 0 || actualWeapon >= 6 || actualRoom < 0 || actualRoom >= 9 || bodyFound < 0 || bodyFound >= 9)
            throw new AbandonModuleException("Unexpected suspect, weapon, room or bodyFound (expected 0–5/0–5/0–8/0–8, got {1}/{2}/{3}/{4}).", _moduleId, actualSuspect, actualWeapon, actualRoom, bodyFound);

        addQuestions(module,
            makeQuestion(Question.MurderSuspect, _Murder,
                new[] { "a suspect but not the murderer" },
                Enumerable.Range(0, 6).Where(suspectIx => skipDisplay[0, suspectIx] == 0 && suspectIx != actualSuspect).Select(suspectIx => names[0, suspectIx]).ToArray()),
            makeQuestion(Question.MurderSuspect, _Murder,
                new[] { "not a suspect" },
                Enumerable.Range(0, 6).Where(suspectIx => skipDisplay[0, suspectIx] == 1).Select(suspectIx => names[0, suspectIx]).ToArray()),

            makeQuestion(Question.MurderWeapon, _Murder,
                new[] { "a potential weapon but not the murder weapon" },
                Enumerable.Range(0, 6).Where(weaponIx => skipDisplay[1, weaponIx] == 0 && weaponIx != actualWeapon).Select(weaponIx => names[1, weaponIx]).ToArray()),
            makeQuestion(Question.MurderWeapon, _Murder,
                new[] { "not a potential weapon" },
                Enumerable.Range(0, 6).Where(weaponIx => skipDisplay[1, weaponIx] == 1).Select(weaponIx => names[1, weaponIx]).ToArray()),

            bodyFound == actualRoom ? null : makeQuestion(Question.MurderBodyFound, _Murder, correctAnswers: new[] { names[2, bodyFound] }));
    }

    private IEnumerable<object> ProcessMysticSquare(KMBombModule module)
    {
        var comp = GetComponent(module, "MysticSquareModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var skull = GetField<Transform>(comp, "Skull", true).Get();
        var knight = GetField<Transform>(comp, "Knight", true).Get();

        while (!skull.gameObject.activeSelf)
            yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_MysticSquare);

        var knightpos = GetIntField(comp, "_knightPos").Get(min: 0, max: 8);
        var skullpos = GetIntField(comp, "_skullPos").Get(min: 0, max: 8);
        var spacepos = Array.IndexOf(GetArrayField<int>(comp, "_field").Get(), 0);

        var answers = new[] { "top left", "top middle", "top right", "middle left", "center", "middle right", "bottom left", "bottom middle", "bottom right" };
        addQuestions(module,
            makeQuestion(Question.MysticSquareKnightSkull, _MysticSquare, new[] { "knight" }, new[] { answers[knightpos] }, answers),
            makeQuestion(Question.MysticSquareKnightSkull, _MysticSquare, new[] { "skull" }, new[] { answers[skullpos] }, answers));

        // If the skull or knight is in the empty space, shrink and then disappear them.
        if (skullpos == spacepos || knightpos == spacepos)
        {
            // Make sure that the last sliding animation finishes
            yield return new WaitForSeconds(0.5f);

            const float duration = 1.5f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                skull.localScale = Vector3.Lerp(new Vector3(0.004f, 0.004f, 0.004f), Vector3.zero, elapsed / duration);
                knight.localScale = Vector3.Lerp(new Vector3(0.004f, 0.004f, 0.004f), Vector3.zero, elapsed / duration);
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        skull.gameObject.SetActive(false);
        knight.gameObject.SetActive(false);
    }

    private IEnumerable<object> ProcessMysteryModule(KMBombModule module)
    {
        var comp = GetComponent(module, "MysteryModuleScript");
        var fldKeyModules = GetListField<KMBombModule>(comp, "keyModules");
        var fldMystifiedModule = GetField<KMBombModule>(comp, "mystifiedModule");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldAnimating = GetField<bool>(comp, "animating");

        while (fldKeyModules.Get(nullAllowed: true) == null)
            yield return null;
        while (fldMystifiedModule.Get(nullAllowed: true) == null)
            yield return null;

        var keyModule = fldKeyModules.Get(ar => ar.Count == 0 ? "empty" : null)[0];
        var mystifiedModule = fldMystifiedModule.Get();

        // Do not ask questions while Souvenir is hidden by Mystery Module.
        if (mystifiedModule == Module)
            _avoidQuestions++;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MysteryModule);

        // Do not ask questions during the solve animation, since Mystery Module modifies the scaling of this module.
        while (fldAnimating.Get())
            yield return new WaitForSeconds(.1f);

        addQuestions(module,
            makeQuestion(Question.MysteryModuleFirstKey, _MysteryModule, correctAnswers: new[] { keyModule.ModuleDisplayName }, preferredWrongAnswers: Bomb.GetSolvableModuleNames().ToArray()),
            makeQuestion(Question.MysteryModuleHiddenModule, _MysteryModule, correctAnswers: new[] { mystifiedModule.ModuleDisplayName }, preferredWrongAnswers: Bomb.GetSolvableModuleNames().ToArray()));

        if (mystifiedModule == Module)
            _avoidQuestions--;
    }

    private IEnumerable<object> ProcessNecronomicon(KMBombModule module)
    {
        var comp = GetComponent(module, "necronomiconScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Necronomicon);

        int[] chapters = GetArrayField<int>(comp, "selectedChapters").Get(expectedLength: 7);
        string[] chaptersString = chapters.Select(x => x.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "first" }, new[] { chaptersString[0] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "second" }, new[] { chaptersString[1] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "third" }, new[] { chaptersString[2] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "fourth" }, new[] { chaptersString[3] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "fifth" }, new[] { chaptersString[4] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "sixth" }, new[] { chaptersString[5] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "seventh" }, new[] { chaptersString[6] }, chaptersString));
    }

    private IEnumerable<object> ProcessNeutralization(KMBombModule module)
    {
        var comp = GetComponent(module, "neutralization");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var acidType = GetIntField(comp, "acidType").Get(min: 0, max: 3);
        var acidVol = GetIntField(comp, "acidVol").Get(av => av < 5 || av > 20 || av % 5 != 0 ? "unexpected acid volume" : null);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Neutralization);

        var colorText = GetField<GameObject>(comp, "colorText", isPublic: true).Get(nullAllowed: true);
        if (colorText != null)
            colorText.SetActive(false);

        addQuestions(module,
            makeQuestion(Question.NeutralizationColor, _Neutralization, correctAnswers: new[] { new[] { "Yellow", "Green", "Red", "Blue" }[acidType] }),
            makeQuestion(Question.NeutralizationVolume, _Neutralization, correctAnswers: new[] { acidVol.ToString() }));
    }

    private IEnumerable<object> ProcessNandMs(KMBombModule module)
    {
        var comp = GetComponent(module, "NandMs");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NandMs);

        var words = GetArrayField<string>(comp, "otherWords").Get();
        var index = GetIntField(comp, "otherwordindex").Get(min: 0, max: words.Length - 1);
        addQuestion(module, Question.NandMsAnswer, correctAnswers: new[] { words[index] });
    }

    private IEnumerable<object> ProcessNavinums(KMBombModule module)
    {
        var comp = GetComponent(module, "navinumsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldStage = GetIntField(comp, "stage");
        var fldDirections = GetListField<int>(comp, "directions");
        var lookUp = GetArrayField<int[]>(comp, "lookUp").Get(expectedLength: 9, validator: ar => ar.Length != 8 ? "expected length 8" : null);
        var directionsSorted = GetListField<int>(comp, "directionsSorted").Get(expectedLength: 4);
        var centerDigit = GetIntField(comp, "center").Get(min: 1, max: 9);

        var curStage = -1;
        var answers = new int[8];
        while (true)
        {
            yield return null;
            var newStage = fldStage.Get();
            if (newStage != curStage)
            {
                if (newStage == 8)
                    break;
                var newDirections = fldDirections.Get();
                if (newDirections.Count != 4)
                    throw new AbandonModuleException("‘directions’ has unexpected length {1} (expected 4).", newDirections.Count);

                answers[newStage] = newDirections.IndexOf(directionsSorted[lookUp[centerDigit - 1][newStage] - 1]);
                if (answers[newStage] == -1)
                    throw new AbandonModuleException("‘directions’ ({0}) does not contain the value from ‘directionsSorted’ ({1}).",
                        newDirections.JoinString(", "), directionsSorted[lookUp[centerDigit - 1][newStage] - 1]);
                curStage = newStage;
            }
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Navinums);

        var directionNames = new[] { "up", "left", "right", "down" };

        var qs = new List<QandA>();
        for (var stage = 0; stage < 8; stage++)
            qs.Add(makeQuestion(Question.NavinumsDirectionalButtons, _Navinums, formatArgs: new[] { ordinal(stage + 1) }, correctAnswers: new[] { directionNames[answers[stage]] }));
        qs.Add(makeQuestion(Question.NavinumsMiddleDigit, _Navinums, correctAnswers: new[] { centerDigit.ToString() }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessNotButton(KMBombModule module)
    {
        var comp = GetComponent(module, "NotButton");
        var propSolved = GetProperty<bool>(comp, "Solved", isPublic: true);
        var propLightColour = GetProperty<object>(comp, "LightColour", isPublic: true); // actual type is an enum
        var propMashCount = GetIntField(comp, "MashCount", isPublic: true);

        var lightColor = 0;
        var mashCount = 0;
        while (!propSolved.Get())
        {
            mashCount = propMashCount.Get();
            lightColor = (int) propLightColour.Get();   // casting boxed enum value to int
            yield return null;  // Don’t wait for .1 seconds so we don’t miss it
        }
        _modulesSolved.IncSafe(_NotButton);

        if (lightColor != 0)
        {
            var strings = _attributes[Question.NotButtonLightColor].AllAnswers;
            if (lightColor <= 0 || lightColor > strings.Length)
                throw new AbandonModuleException("‘LightColour’ is out of range ({0}).", lightColor);
            addQuestion(module, Question.NotButtonLightColor, correctAnswers: new[] { strings[lightColor - 1] });
        }
        else if (mashCount > 1)
        {
            var wrongAnswerStrings = Enumerable.Range(0, 20).Select(_ => Rnd.Range(10, 100)).Where(i => i != mashCount).Distinct().Take(5).Select(i => i.ToString()).ToArray();
            addQuestion(module, Question.NotButtonMashCount, correctAnswers: new[] { mashCount.ToString() }, preferredWrongAnswers: wrongAnswerStrings);
        }
        else
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Not the Button because the button was tapped (or I missed the light color).", _moduleId);
            _legitimatelyNoQuestions.Add(module);
        }
    }

    private IEnumerable<object> ProcessNotKeypad(KMBombModule module)
    {
        var comp = GetComponent(module, "NotKeypad");
        var connectorComponent = GetComponent(module, "NotVanillaModulesLib.NotKeypadConnector");
        var propSolved = GetProperty<bool>(comp, "Solved", true);

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotKeypad);

        var strings = GetAnswers(Question.NotKeypadColor);
        var colours = GetField<Array>(comp, "sequenceColours").Get(ar => ar.Cast<int>().Any(v => v <= 0 || v > strings.Length) ? "out of range" : null);
        var buttons = GetArrayField<int>(comp, "sequenceButtons").Get(expectedLength: colours.Length);
        var symbols = GetField<Array>(connectorComponent, "symbols").Get(ar => ar.Cast<int>().Any(v => v < 0 || v > KeypadSprites.Length) ? "out of range" : null);
        var sprites = symbols.Cast<int>().Select(i => KeypadSprites[i]).ToArray();

        var qs = new List<QandA>();
        for (var stage = 0; stage < colours.Length; stage++)
        {
            qs.Add(makeQuestion(Question.NotKeypadColor, _NotKeypad, new[] { ordinal(stage + 1) }, new[] { strings[(int) colours.GetValue(stage) - 1] }));
            qs.Add(makeQuestion(Question.NotKeypadSymbol, _NotKeypad, new[] { ordinal(stage + 1) }, new[] { KeypadSprites[(int) symbols.GetValue(buttons[stage])] }, sprites));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessNotMaze(KMBombModule module)
    {
        var component = GetComponent(module, "NotMaze");
        var propSolved = GetProperty<bool>(component, "Solved", true);

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotMaze);

        addQuestion(module, Question.NotMazeStartingDistance, correctAnswers: new[] { GetIntField(component, "distance").Get().ToString() });
    }

    private IEnumerable<object> ProcessNotMorseCode(KMBombModule module)
    {
        var component = GetComponent(module, "NotMorseCode");
        var propSolved = GetProperty<bool>(component, "Solved", true);

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotMorseCode);

        var words = GetArrayField<string>(component, "words").Get();
        var channels = GetArrayField<int>(component, "correctChannels").Get();
        var columns = GetStaticField<string[][]>(component.GetType(), "defaultColumns").Get();

        addQuestions(module, Enumerable.Range(0, 5).Select(stage => makeQuestion(
            question: Question.NotMorseCodeWord,
            moduleKey: _NotMorseCode,
            formatArgs: new[] { ordinal(stage + 1) },
            correctAnswers: new[] { words[channels[stage]] },
            preferredWrongAnswers: words.Concat(Enumerable.Range(0, 50).Select(_ => columns.PickRandom().PickRandom())).Except(new[] { words[channels[stage]] }).Distinct().Take(8).ToArray())));
    }

    private IEnumerable<object> ProcessNotSimaze(KMBombModule module)
    {
        var comp = GetComponent(module, "NotSimaze");
        var propSolved = GetProperty<bool>(comp, "Solved", isPublic: true);
        var fldMazeIndex = GetIntField(comp, "mazeIndex");

        var colours = _attributes[Question.NotSimazeMaze].AllAnswers;
        var startPositionArray = new[] { string.Format("({0}, {1})", colours[GetIntField(comp, "x").Get()], colours[GetIntField(comp, "y").Get()]) };

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotSimaze);

        var goalPositionArray = new[] { string.Format("({0}, {1})", colours[GetIntField(comp, "goalX").Get()], colours[GetIntField(comp, "goalY").Get()]) };

        addQuestions(module,
            makeQuestion(Question.NotSimazeMaze, _NotSimaze, correctAnswers: new[] { colours[fldMazeIndex.Get()] }),
            makeQuestion(Question.NotSimazeStart, _NotSimaze, correctAnswers: startPositionArray, preferredWrongAnswers: goalPositionArray),
            makeQuestion(Question.NotSimazeGoal, _NotSimaze, correctAnswers: goalPositionArray, preferredWrongAnswers: startPositionArray));
    }

    private IEnumerable<object> ProcessNotWhosOnFirst(KMBombModule module)
    {
        var comp = GetComponent(module, "NotWhosOnFirst");
        var propSolved = GetProperty<bool>(comp, "Solved", true);
        var fldPositions = GetArrayField<int>(comp, "rememberedPositions");
        var fldLabels = GetArrayField<string>(comp, "rememberedLabels");
        var fldSum = GetIntField(comp, "stage2Sum");

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotWhosOnFirst);

        var positions = _attributes[Question.NotWhosOnFirstPressedPosition].AllAnswers;
        var sumCorrectAnswers = new[] { fldSum.Get().ToString() };

        var qs = new List<QandA>();
        for (var i = 0; i < 4; i++)
        {
            qs.Add(makeQuestion(Question.NotWhosOnFirstPressedPosition, _NotWhosOnFirst, new[] { (i + 1).ToString() }, new[] { positions[fldPositions.Get()[i]] }));
            qs.Add(makeQuestion(Question.NotWhosOnFirstPressedLabel, _NotWhosOnFirst, new[] { (i + 1).ToString() }, new[] { fldLabels.Get()[i] }));
        }
        for (var i = 4; i < 6; i++)
        {
            qs.Add(makeQuestion(Question.NotWhosOnFirstReferencePosition, _NotWhosOnFirst, new[] { (i - 1).ToString() }, new[] { positions[fldPositions.Get()[i]] }));
            qs.Add(makeQuestion(Question.NotWhosOnFirstReferenceLabel, _NotWhosOnFirst, new[] { (i - 1).ToString() }, new[] { fldLabels.Get()[i] }));
        }
        qs.Add(makeQuestion(Question.NotWhosOnFirstSum, _NotWhosOnFirst, correctAnswers: sumCorrectAnswers));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessNumberedButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "NumberedButtonsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_NumberedButtons);

        var numbers = GetArrayField<int>(comp, "buttonNums").Get(expectedLength: 16, validator: n => n < 1 || n > 100 ? "expected 1–100" : null);
        addQuestions(module, numbers.Select((n, ix) => makeQuestion(Question.NumberedButtonsLabels, _NumberedButtons, new[] { (ix + 1).ToString() }, correctAnswers: new[] { n.ToString() })));
    }

    private IEnumerable<object> ProcessObjectShows(KMBombModule module)
    {
        var comp = GetComponent(module, "objectShows");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ObjectShows);

        var contestantNames = GetArrayField<string>(comp, "charnames", isPublic: true).Get();
        var solutionObjs = GetField<Array>(comp, "solution").Get(ar => ar.Length != 5 ? "expected length 5" : ar.Cast<object>().Any(obj => obj == null) ? "contains null" : null).Cast<object>().ToArray();
        var fldId = GetIntField(solutionObjs[0], "id", isPublic: true);
        var solutionNames = solutionObjs.Select(c => contestantNames[fldId.GetFrom(c, min: 0, max: contestantNames.Length - 1)]).ToArray();
        addQuestion(module, Question.ObjectShowsContestants, correctAnswers: solutionNames, preferredWrongAnswers: contestantNames);
    }

    private IEnumerable<object> ProcessOddOneOut(KMBombModule module)
    {
        var comp = GetComponent(module, "OddOneOutModule");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);

        var stages = GetField<Array>(comp, "_stages").Get(ar => ar.Length != 6 ? "expected length 6" : ar.Cast<object>().Any(obj => obj == null) ? "contains null" : null);
        var btnNames = new[] { "top-left", "top-middle", "top-right", "bottom-left", "bottom-middle", "bottom-right" };
        var stageBtn = stages.Cast<object>().Select(x => GetIntField(x, "CorrectIndex", isPublic: true).Get(min: 0, max: btnNames.Length - 1)).ToArray();

        _modulesSolved.IncSafe(_OddOneOut);
        addQuestions(module,
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "first" }, new[] { btnNames[stageBtn[0]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "second" }, new[] { btnNames[stageBtn[1]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "third" }, new[] { btnNames[stageBtn[2]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "fourth" }, new[] { btnNames[stageBtn[3]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "fifth" }, new[] { btnNames[stageBtn[4]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "sixth" }, new[] { btnNames[stageBtn[5]] }));
    }

    private IEnumerable<object> ProcessOnlyConnect(KMBombModule module)
    {
        var comp = GetComponent(module, "OnlyConnectModule");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");
        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var hieroglyphsDisplayed = GetArrayField<int>(comp, "_hieroglyphsDisplayed").Get(expectedLength: 6, validator: v => v < 0 || v > 5 ? "expected range 0–5" : null);

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_OnlyConnect);

        var hieroglyphs = new[] { "Two Reeds", "Lion", "Twisted Flax", "Horned Viper", "Water", "Eye of Horus" };
        var positions = new[] { "top left", "top middle", "top right", "bottom left", "bottom middle", "bottom right" };
        addQuestions(module, positions.Select((p, i) => makeQuestion(Question.OnlyConnectHieroglyphs, _OnlyConnect, new[] { p }, new[] { hieroglyphs[hieroglyphsDisplayed[i]] })));
    }

    private IEnumerable<object> ProcessOrangeArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "OrangeArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldMoves = GetArrayField<string>(comp, "moves");
        var fldStage = GetIntField(comp, "stage");

        // The module does not modify the arrays; it instantiates a new one for each stage.
        var correctMoves = new string[3][];

        var buttons = GetArrayField<KMSelectable>(comp, "buttons", isPublic: true).Get();
        var prevButtonInteracts = buttons.Select(b => b.OnInteract).ToArray();
        for (int i = 0; i < buttons.Length; i++)
        {
            var prevInteract = prevButtonInteracts[i];
            buttons[i].OnInteract = delegate
            {
                var ret = prevInteract();
                var st = fldStage.Get();
                if (st < 1 || st > 3)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Orange Arrows because ‘stage’ was out of range: {1}.", _moduleId, st);
                    correctMoves = null;
                    for (int j = 0; j < buttons.Length; j++)
                        buttons[j].OnInteract = prevButtonInteracts[j];
                }
                else
                {
                    // We need to capture the array at each button press because the user might get a strike and the array might change.
                    // Avoid throwing an exception within the button handler
                    correctMoves[st - 1] = fldMoves.Get(nullAllowed: true);
                }
                return ret;
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_OrangeArrows);

        if (correctMoves == null)   // an error message has already been output
            yield break;

        for (int i = 0; i < buttons.Length; i++)
            buttons[i].OnInteract = prevButtonInteracts[i];

        var directions = new[] { "UP", "RIGHT", "DOWN", "LEFT" };
        if (correctMoves.Any(arr => arr == null || arr.Any(dir => !directions.Contains(dir))))
            throw new AbandonModuleException("One of the move arrays has an unexpected value: [{0}].",
                correctMoves.Select(arr => arr == null ? "null" : string.Format("[{0}]", arr.JoinString(", "))).JoinString(", "));

        var qs = new List<QandA>();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                qs.Add(makeQuestion(Question.OrangeArrowsSequences, _OrangeArrows, new[] { ordinal(j + 1), ordinal(i + 1) }, new[] { correctMoves[i][j].Substring(0, 1) + correctMoves[i][j].Substring(1).ToLowerInvariant() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessOrderedKeys(KMBombModule module)
    {
        var comp = GetComponent(module, "OrderedKeysScript");
        var fldInfo = GetArrayField<int[]>(comp, "info");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldStage = GetIntField(comp, "stage");

        var stage = 0;
        var moduleData = new int[3][][];

        while (!fldSolved.Get())
        {
            var info = fldInfo.Get(expectedLength: 6, validator: arr => arr == null ? "null" : arr.Length != 4 ? "expected length 4" : null);
            var newStage = fldStage.Get(min: 1, max: 3);
            if (stage != newStage || moduleData[newStage - 1] == null || Enumerable.Range(0, 6).All(ix => info[ix].SequenceEqual(moduleData[newStage - 1][ix])))
            {
                stage = newStage;
                moduleData[stage - 1] = info.Select(arr => arr.ToArray()).ToArray(); // Take a copy of the array.
            }
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_OrderedKeys);

        var colors = new string[6] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" };

        var qs = new List<QandA>();
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 6; j++)
            {
                qs.Add(makeQuestion(Question.OrderedKeysColors, _OrderedKeys, new[] { (i + 1).ToString(), ordinal(j + 1) }, new[] { colors[moduleData[i][j][0]] }));
                qs.Add(makeQuestion(Question.OrderedKeysLabels, _OrderedKeys, new[] { (i + 1).ToString(), ordinal(j + 1) }, new[] { moduleData[i][j][3].ToString() }));
                qs.Add(makeQuestion(Question.OrderedKeysLabelColors, _OrderedKeys, new[] { (i + 1).ToString(), ordinal(j + 1) }, new[] { colors[moduleData[i][j][1]] }));
            }
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessOrientationCube(KMBombModule module)
    {
        var comp = GetComponent(module, "OrientationModule");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_OrientationCube);

        var initialVirtualViewAngle = GetField<float>(comp, "initialVirtualViewAngle").Get();
        var initialAnglePos = Array.IndexOf(new[] { 0f, 90f, 180f, 270f }, initialVirtualViewAngle);
        if (initialAnglePos == -1)
            throw new AbandonModuleException("‘initialVirtualViewAngle’ has unexpected value: {0}", initialVirtualViewAngle);

        addQuestion(module, Question.OrientationCubeInitialObserverPosition, correctAnswers: new[] { new[] { "front", "left", "back", "right" }[initialAnglePos] });
    }

    private IEnumerable<object> ProcessPalindromes(KMBombModule module)
    {
        var comp = GetComponent(module, "Palindromes");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldX = GetField<string>(comp, "x");
        var fldY = GetField<string>(comp, "y");
        var fldZ = GetField<string>(comp, "z");
        var fldN = GetField<string>(comp, "n");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Palindromes);

        var vars = new[] { fldN, fldX, fldY, fldZ };
        byte randomVar = (byte) Rnd.Range(0, vars.Length);
        byte randomInd = (byte) Rnd.Range(0, randomVar < 2 ? 5 : 4);  // 5 if x or n, else 4
        string numString = vars[randomVar].Get();
        char digit = numString[numString.Length - 1 - randomInd];
        if (digit < '0' || digit > '9')
            throw new AbandonModuleException("The chosen character was unexpected ('{0}').", digit);

        string[] labels = new string[] { "the screen", "X", "Y", "Z" };
        addQuestion(module, Question.PalindromesNumbers, new[] { labels[randomVar], ordinal(randomInd + 1) }, correctAnswers: new[] { digit.ToString() });
    }

    private IEnumerable<object> ProcessPartialDerivatives(KMBombModule module)
    {
        var comp = GetComponent(module, "PartialDerivativesScript");
        var fldLeds = GetArrayField<int>(comp, "ledIndex");

        var display = GetField<TextMesh>(comp, "display", isPublic: true).Get();
        var terms = display.text.Split('\n').Select(term => Regex.Replace(Regex.Replace(term.Trim(), @"^(f.*?=|\+) ", ""), @"^- ", "−")).ToArray();
        if (terms.Length != 3)
            throw new AbandonModuleException(@"The display does not appear to contain three terms: ""{0}""", _moduleId, display.text.Replace("\r", "").Replace("\n", "\\n"));

        var vars = new[] { "x", "y", "z" };
        var exponentStrs = new[] { "²", "³", "⁴", "⁵" };
        var writeTerm = new Func<int, bool, int[], string>((int coeff, bool negative, int[] exps) =>
        {
            if (coeff == 0)
                return "0";

            var function = negative ? "−" : "";
            if (coeff > 1)
                function += coeff.ToString();
            for (int j = 0; j < 3; j++)
            {
                if (exps[j] != 0)
                {
                    function += vars[j];
                    if (exps[j] > 1)
                        function += exponentStrs[exps[j] - 2];
                }
            }
            return function;
        });

        var wrongAnswers = new HashSet<string>();
        while (wrongAnswers.Count < 3)
        {
            var exps = new int[3];
            for (int j = 0; j < 3; j++)
                exps[j] = Rnd.Range(0, 6);
            if (exps.All(e => e == 0))
                exps[Rnd.Range(0, 3)] = Rnd.Range(1, 6);
            var wrongTerm = writeTerm(Rnd.Range(1, 10), Rnd.Range(0, 2) != 0, exps);
            if (!terms.Contains(wrongTerm))
                wrongAnswers.Add(wrongTerm);
        }

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        while (!isSolved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PartialDerivatives);

        var leds = fldLeds.Get(expectedLength: 3, validator: l => l < 0 || l > 5 ? "expected range 0–5" : null);
        var colorNames = new[] { "blue", "green", "orange", "purple", "red", "yellow" };
        var qs = new List<QandA>();
        for (var stage = 0; stage < 3; stage++)
            qs.Add(makeQuestion(Question.PartialDerivativesLedColors, _PartialDerivatives, formatArgs: new[] { (stage + 1).ToString() }, correctAnswers: new[] { colorNames[leds[stage]] }));
        for (var term = 0; term < 3; term++)
            qs.Add(makeQuestion(Question.PartialDerivativesTerms, _PartialDerivatives, formatArgs: new[] { ordinal(term + 1) }, correctAnswers: new[] { terms[term] }, preferredWrongAnswers: wrongAnswers.ToArray()));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessPassportControl(KMBombModule module)
    {
        var comp = GetComponent(module, "passportControlScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldPassages = GetIntField(comp, "passages");
        var fldExpiration = GetArrayField<int>(comp, "expiration");
        var stamps = GetArrayField<KMSelectable>(comp, "stamps", isPublic: true).Get();
        var textToHide1 = GetArrayField<GameObject>(comp, "passport", isPublic: true).Get(validator: objs => objs.Any(go => go.GetComponent<TextMesh>() == null) ? "doesn’t have a TextMesh component" : null);
        var textToHide2 = GetField<GameObject>(comp, "ticket", isPublic: true).Get(go => go.GetComponent<TextMesh>() == null ? "doesn’t have a TextMesh component" : null);

        var expirationDates = new List<int>();

        for (int i = 0; i < stamps.Length; i++)
        {
            var oldHandler = stamps[i].OnInteract;
            stamps[i].OnInteract = delegate
            {
                // Only add the expiration date if there is no error. The error is caught later when the length of ‘expirationDates’ is checked.
                // Avoid throwing exceptions inside of the button handler
                var date = fldExpiration.Get(nullAllowed: true);
                if (date == null || date.Length != 3)
                    return oldHandler();

                var year = date[2];
                var passages = fldPassages.Get();
                var ret = oldHandler();
                if (fldPassages.Get() == passages) // player got strike, ignoring retrieved info
                    return ret;

                expirationDates.Add(year);
                return ret;
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        if (expirationDates.Count != 3)
            throw new AbandonModuleException("The number of retrieved sets of information was {0} (expected 3).", expirationDates.Count);

        for (int i = 0; i < textToHide1.Length; i++)
            textToHide1[i].GetComponent<TextMesh>().text = "";
        textToHide2.GetComponent<TextMesh>().text = "";

        var altDates = new List<string[]>();

        for (int i = 0; i < expirationDates.Count; i++)
        {
            altDates.Add(new string[6]);
            int startVal = expirationDates[i] - Rnd.Range(0, 6);
            for (int j = 0; j < altDates[i].Length; j++)
                altDates[i][j] = (startVal + j).ToString();
        }

        _modulesSolved.IncSafe(_PassportControl);
        addQuestions(module,
            makeQuestion(Question.PassportControlPassenger, _PassportControl, new[] { "first" }, new[] { expirationDates[0].ToString() }, altDates[0]),
            makeQuestion(Question.PassportControlPassenger, _PassportControl, new[] { "second" }, new[] { expirationDates[1].ToString() }, altDates[1]),
            makeQuestion(Question.PassportControlPassenger, _PassportControl, new[] { "third" }, new[] { expirationDates[2].ToString() }, altDates[2]));
    }

    private IEnumerable<object> ProcessPatternCube(KMBombModule module)
    {
        var comp = GetComponent(module, "PatternCubeModule");
        var selectableSymbols = GetField<Array>(comp, "_selectableSymbols").Get(ar => ar.Length != 5 ? "expected length 5" : null);
        var selectableSymbolObjects = GetArrayField<MeshRenderer>(comp, "_selectableSymbolObjs").Get(expectedLength: 5);
        var placeableSymbolObjects = GetArrayField<MeshRenderer>(comp, "_placeableSymbolObjs").Get(expectedLength: 6);
        var highlightPos = GetIntField(comp, "_highlightedPosition").Get(min: 0, max: 4);

        // Wait for it to be solved.
        while (selectableSymbols.Cast<object>().Any(obj => obj != null))
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PatternCube);

        var symbols = selectableSymbolObjects.Concat(placeableSymbolObjects.Where(r => r.gameObject.activeSelf))
            .Select(r => PatternCubeSprites[int.Parse(r.sharedMaterial.mainTexture.name.Substring(6))]).ToArray();
        addQuestion(module, Question.PatternCubeHighlightedSymbol, null, new[] { symbols[highlightPos] }, symbols);
    }

    private IEnumerable<object> ProcessPerspectivePegs(KMBombModule module)
    {
        var comp = GetComponent(module, "PerspectivePegsModule");
        var fldIsComplete = GetField<bool>(comp, "isComplete");
        while (!fldIsComplete.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PerspectivePegs);

        int keyNumber = 0;
        string keyColour;
        char prevChar = '\0';
        foreach (var letter in Bomb.GetSerialNumberLetters())
        {
            if (prevChar == 0)
                prevChar = letter;
            else
            {
                keyNumber += Math.Abs(letter - prevChar);
                prevChar = '\0';
            }
        }
        switch (keyNumber % 10)
        {
            case 0: case 3: keyColour = "ColourRed"; break;
            case 4: case 9: keyColour = "ColourYellow"; break;
            case 1: case 7: keyColour = "ColourGreen"; break;
            case 5: case 8: keyColour = "ColourBlue"; break;
            case 2: case 6: keyColour = "ColourPurple"; break;
            default: yield break;
        }

        var colourMeshes = GetField<MeshRenderer[,]>(comp, "ColourMeshes").Get();
        var pegIndex = Enumerable.Range(0, 5).IndexOf(px => Enumerable.Range(0, 5).Count(i => colourMeshes[px, i].sharedMaterial.name.StartsWith(keyColour)) >= 3);
        if (pegIndex == -1)
            throw new AbandonModuleException("The key peg couldn't be found (the key colour was {0}).", keyColour);

        addQuestions(module, Enumerable.Range(0, 3).Select(i => makeQuestion(
             Question.PerspectivePegsSolution,
            _PerspectivePegs,
            correctAnswers: new[] { PerspectivePegsSprites[pegIndex] },
            preferredWrongAnswers: PerspectivePegsSprites)));
    }

    private IEnumerable<object> ProcessPie(KMBombModule module)
    {
        var comp = GetComponent(module, "PieScript");
        var fldSolved = GetField<bool>(comp, "solveCoroutineStarted");
        var digits = GetArrayField<string>(comp, "codes").Get(expectedLength: 5);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Pie);

        addQuestions(module, digits.Select((digit, ix) => makeQuestion(Question.PieDigits, _Pie, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { digit }, preferredWrongAnswers: digits)));
    }

    private IEnumerable<object> ProcessPigpenCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "PigpenCycleScript", Question.PigpenCycleWord, _PigpenCycle);
    }

    private IEnumerable<object> ProcessPlaceholderTalk(KMBombModule module)
    {
        var comp = GetComponent(module, "placeholderTalk");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PlaceholderTalk);

        var answer = GetField<byte>(comp, "answerId").Get(b => b < 0 || b > 16 ? "expected range 0–16" : null) + 1;
        var firstPhrase = GetArrayField<string>(comp, "firstPhrase").Get();
        var firstString = GetField<string>(comp, "firstString").Get(str => !firstPhrase.Contains(str) ? string.Format("expected string to be contained in “{0}” (‘firstPhrase’)", firstPhrase) : null);
        var ordinals = GetArrayField<string>(comp, "ordinals").Get();
        var currentOrdinal = GetField<string>(comp, "currentOrdinal").Get(str => !ordinals.Contains(str) ? string.Format("expected string to be contained in “{0}” (‘ordinals’)", ordinals) : null);
        var previousModules = GetField<sbyte>(comp, "previousModules").Get();

        var qs = new List<QandA>();

        // Because the number of solved modules could be any number, the second phrase question should be deactivated if previousModule is either 1 or -1, meaning that they apply to the numbers
        if (previousModules == 0)
            qs.Add(makeQuestion(Question.PlaceholderTalkSecondPhrase, _PlaceholderTalk, correctAnswers: new[] { answer.ToString() }));

        qs.Add(makeQuestion(Question.PlaceholderTalkFirstPhrase, _PlaceholderTalk, correctAnswers: new[] { firstString }, preferredWrongAnswers: firstPhrase));
        qs.Add(makeQuestion(Question.PlaceholderTalkOrdinal, _PlaceholderTalk, correctAnswers: new[] { currentOrdinal }, preferredWrongAnswers: ordinals));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessPlanets(KMBombModule module)
    {
        var comp = GetComponent(module, "planetsModScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var planetShown = GetIntField(comp, "planetShown").Get(0, 9);
        var stripColors = GetArrayField<int>(comp, "stripColours").Get(expectedLength: 5, validator: x => x < 0 || x > 8 ? "expected range 0–8" : null);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Planets);

        var stripNames = new[] { "Aqua", "Blue", "Green", "Lime", "Orange", "Red", "Yellow", "White", "Off" };
        addQuestions(module,
            stripColors.Select((strip, count) => makeQuestion(Question.PlanetsStrips, _Planets, new[] { ordinal(count + 1) }, new[] { stripNames[strip] }))
                .Concat(new[] { makeQuestion(Question.PlanetsPlanet, _Planets, correctAnswers: new[] { PlanetsSprites[planetShown] }, preferredWrongAnswers: (DateTime.Now.Month == 4 && DateTime.Now.Day == 1) ? PlanetsSprites : PlanetsSprites.Take(PlanetsSprites.Length - 2).ToArray()) }));
    }

    private IEnumerable<object> ProcessPlayfairCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "PlayfairCycleScript", Question.PlayfairCycleWord, _PlayfairCycle);
    }

    private IEnumerable<object> ProcessPoetry(KMBombModule module)
    {
        var comp = GetComponent(module, "PoetryModule");
        var fldStage = GetIntField(comp, "currentStage");
        var fldStageCount = GetIntField(comp, "stageCount", isPublic: true);

        var answers = new List<string>();
        var selectables = GetArrayField<KMSelectable>(comp, "wordSelectables", isPublic: true).Get(expectedLength: 6);
        var wordTextMeshes = GetArrayField<TextMesh>(comp, "words", isPublic: true).Get(expectedLength: 6);

        for (int i = 0; i < 6; i++)
        {
            var j = i;
            var oldHandler = selectables[i].OnInteract;
            selectables[i].OnInteract = delegate
            {
                var prevStage = fldStage.Get();
                var word = wordTextMeshes[j].text;
                var ret = oldHandler();

                if (fldStage.Get() > prevStage)
                    answers.Add(word);

                return ret;
            };
        }

        while (fldStage.Get() < fldStageCount.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Poetry);

        if (answers.Count != fldStageCount.Get())
            throw new AbandonModuleException("The number of answers captured is not equal to the number of stages played ({0}). Answers were: [{1}]",
                fldStageCount.Get(), answers.JoinString(", "));

        addQuestions(module, answers.Select((ans, st) => makeQuestion(Question.PoetryAnswers, _Poetry, formatArgs: new[] { ordinal(st + 1) }, correctAnswers: new[] { ans }, preferredWrongAnswers: answers.ToArray())));
    }

    private IEnumerable<object> ProcessPolyhedralMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "PolyhedralMazeModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PolyhedralMaze);

        addQuestion(module, Question.PolyhedralMazeStartPosition, null, new[] { GetIntField(comp, "_startFace").Get().ToString() });
    }

    private IEnumerable<object> ProcessProbing(KMBombModule module)
    {
        var comp = GetComponent(module, "ProbingModule");
        var fldSolved = GetField<bool>(comp, "bSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Probing);

        var display = GetField<TextMesh>(comp, "display", isPublic: true).Get();

        // Blank out the display so that the user cannot see the readout for the solution wires
        display.text = "";

        // Prevent the user from interacting with the wires after solving
        foreach (var selectable in GetArrayField<KMSelectable>(comp, "selectables", isPublic: true).Get(expectedLength: 6))
            selectable.OnInteract = delegate { return false; };

        var wireNames = new[] { "red-white", "yellow-black", "green", "gray", "yellow-red", "red-blue" };
        var frequencyDic = new Dictionary<int, string> { { 7, "60Hz" }, { 11, "50Hz" }, { 13, "22Hz" }, { 14, "10Hz" } };
        var wireFrequenciesRaw = GetField<Array>(comp, "mWires").Get(ar => ar.Length != 6 ? "expected length 6" : ar.Cast<int>().Any(v => !frequencyDic.ContainsKey(v)) ? "contains unknown frequency value" : null);
        var wireFrequencies = wireFrequenciesRaw.Cast<int>().Select(val => frequencyDic[val]).ToArray();

        addQuestions(module, wireFrequencies.Select((wf, ix) => makeQuestion(Question.ProbingFrequencies, _Probing, new[] { wireNames[ix] }, new[] { wf })));
    }

    private IEnumerable<object> ProcessPurpleArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "PurpleArrowsScript");

        // The module sets moduleSolved to true at the start of its solve animation but before it is actually marked as solved.
        // Therefore, we use OnPass to wait for the end of that animation and then set the text to “SOLVED” afterwards.
        var solved = false;
        module.OnPass += delegate { solved = true; return false; };

        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PurpleArrows);

        string finishWord = GetField<string>(comp, "finish").Get(str => str.Length != 6 ? "expected length 6" : null);
        string[] wordList = GetArrayField<string>(comp, "words").Get(expectedLength: 9 * 13);

        if (!wordList.Contains(finishWord))
            throw new AbandonModuleException("‘wordList’ does not contain ‘finishWord’: [Length: {1}, finishWord: {2}].", wordList.Length, finishWord);

        var wordScreen = GetField<GameObject>(comp, "wordDisplay", isPublic: true).Get();
        var wordScreenTextMesh = wordScreen.GetComponent<TextMesh>();
        if (wordScreenTextMesh == null)
            throw new AbandonModuleException("‘wordDisplay’ does not have a TextMesh component.");
        wordScreenTextMesh.text = "SOLVED";

        addQuestion(module, Question.PurpleArrowsFinish, correctAnswers: new[] { Regex.Replace(finishWord, @"(?<!^).", m => m.Value.ToLowerInvariant()) }, preferredWrongAnswers: wordList.Select(w => w[0] + w.Substring(1).ToLowerInvariant()).ToArray());
    }

    private IEnumerable<object> ProcessQuintuples(KMBombModule module)
    {
        var comp = GetComponent(module, "quintuplesScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Quintuples);

        var numbers = GetArrayField<int>(comp, "cyclingNumbers", isPublic: true).Get(expectedLength: 25, validator: n => n < 1 || n > 10 ? "expected range 1–10" : null);
        var colors = GetArrayField<string>(comp, "chosenColorsName", isPublic: true).Get(expectedLength: 25);
        var colorCounts = GetArrayField<int>(comp, "numberOfEachColour", isPublic: true).Get(expectedLength: 5, validator: cc => cc < 0 || cc > 25 ? "expected range 0–25" : null);
        var colorNames = GetArrayField<string>(comp, "potentialColorsName", isPublic: true).Get(expectedLength: 5);

        addQuestions(module,
            numbers.Select((n, ix) => makeQuestion(Question.QuintuplesNumbers, _Quintuples, new[] { ordinal(ix % 5 + 1), ordinal(ix / 5 + 1) }, new[] { (n % 10).ToString() })).Concat(
            colors.Select((color, ix) => makeQuestion(Question.QuintuplesColors, _Quintuples, new[] { ordinal(ix % 5 + 1), ordinal(ix / 5 + 1) }, new[] { color }))).Concat(
            colorCounts.Select((cc, ix) => makeQuestion(Question.QuintuplesColorCounts, _Quintuples, new[] { colorNames[ix] }, new[] { cc.ToString() }))));
    }

    private IEnumerable<object> ProcessRecoloredSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "Recolored_Switches");

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        while (!isSolved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_RecoloredSwitches);

        var colorNames = new Dictionary<char, string>
        {
            { 'R', "red" },
            { 'G', "green" },
            { 'B', "blue" },
            { 'T', "turquoise" },
            { 'O', "orange" },
            { 'P', "purple" },
            { 'W', "white" }
        };
        var ledColors = GetField<StringBuilder>(comp, "LEDsColorsString").Get(sb => sb.Length != 10 ? "expected length 10" : Enumerable.Range(0, 10).Any(ix => !colorNames.ContainsKey(sb[ix])) ? string.Format("expected {0}", colorNames.Keys.JoinString()) : null);
        addQuestions(module, Enumerable.Range(0, 10).Select(ix => makeQuestion(Question.RecoloredSwitchesLedColors, _RecoloredSwitches, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { colorNames[ledColors[ix]] })));
    }

    private IEnumerable<object> ProcessRedArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "RedArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_RedArrows);

        addQuestion(module, Question.RedArrowsStartNumber, correctAnswers: new[] { GetIntField(comp, "start").Get(min: 0, max: 9).ToString() });
    }

    private IEnumerable<object> ProcessRetirement(KMBombModule module)
    {
        var comp = GetComponent(module, "retirementScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Retirement);

        string[] homes = GetArrayField<string>(comp, "retirementHomeOptions", isPublic: true).Get();
        string[] available = GetArrayField<string>(comp, "selectedHomes").Get();
        string correct = GetField<string>(comp, "correctHome").Get(str => str == "" ? "empty" : null);
        addQuestion(module, Question.RetirementHouses, correctAnswers: available.Where(x => x != correct).ToArray(), preferredWrongAnswers: homes);
    }

    private IEnumerable<object> ProcessReverseMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "reverseMorseScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var message1 = GetListField<string>(comp, "selectedLetters1", isPublic: true).Get(expectedLength: 6);
        var message2 = GetListField<string>(comp, "selectedLetters2", isPublic: true).Get(expectedLength: 6);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ReverseMorse);

        var qs = new List<QandA>();
        for (int i = 0; i < 6; i++)
        {
            qs.Add(makeQuestion(Question.ReverseMorseCharacters, _ReverseMorse, new[] { ordinal(i + 1), "first" }, new[] { message1[i] }, message1.ToArray()));
            qs.Add(makeQuestion(Question.ReverseMorseCharacters, _ReverseMorse, new[] { ordinal(i + 1), "second" }, new[] { message2[i] }, message2.ToArray()));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessRGBMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "RGBMazeScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_RGBMaze);

        var keyPos = GetArrayField<int[]>(comp, "keylocations").Get(expectedLength: 3, validator: key => key.Length != 2 ? "expected length 2" : key.Any(number => number < 0 || number > 7) ? "expected range 0–7" : null);
        var mazeNum = GetArrayField<int[]>(comp, "mazenumber").Get(expectedLength: 3, validator: maze => maze.Length != 2 ? "expected length 2" : maze[0] < 0 || maze[0] > 9 ? "expected maze[0] in range 0–9" : null);
        var exitPos = GetArrayField<int>(comp, "exitlocation").Get(expectedLength: 3);

        if (exitPos[1] < 0 || exitPos[1] > 7 || exitPos[2] < 0 || exitPos[2] > 7)
            throw new AbandonModuleException("‘exitPos’ contains invalid coordinate: ({0},{1})", exitPos[2], exitPos[1]);

        string[] colors = { "red", "green", "blue" };

        var qs = new List<QandA>();

        for (int index = 0; index < 3; index++)
        {
            qs.Add(makeQuestion(Question.RGBMazeKeys, _RGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { "ABCDEFGH"[keyPos[index][1]] + (keyPos[index][0] + 1).ToString() }));
            qs.Add(makeQuestion(Question.RGBMazeNumber, _RGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { mazeNum[index][0].ToString() }));
        }

        qs.Add(makeQuestion(Question.RGBMazeExit, _RGBMaze,
            correctAnswers: new[] { "ABCDEFGH"[exitPos[2]] + (exitPos[1] + 1).ToString() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessRhythms(KMBombModule module)
    {
        var comp = GetComponent(module, "Rhythms");
        var fldSolved = GetField<bool>(comp, "isSolved", isPublic: true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Rhythms);

        var color = GetIntField(comp, "lightColor").Get(min: 0, max: 3);
        addQuestion(module, Question.RhythmsColor, correctAnswers: new[] { new[] { "Blue", "Red", "Green", "Yellow" }[color] });
    }

    private IEnumerable<object> ProcessRoleReversal(KMBombModule module)
    {
        var comp = GetComponent(module, "roleReversal");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_RoleReversal);

        var redWires = GetListField<byte>(comp, "redWires").Get(lst => lst.Count > 7 ? "expected 7 or fewer elements" : null);
        var orangeWires = GetListField<byte>(comp, "orangeWires").Get(lst => lst.Count > 7 ? "expected 7 or fewer elements" : null);
        var yellowWires = GetListField<byte>(comp, "yellowWires").Get(lst => lst.Count > 7 ? "expected 7 or fewer elements" : null);
        var greenWires = GetListField<byte>(comp, "greenWires").Get(lst => lst.Count > 7 ? "expected 7 or fewer elements" : null);
        var blueWires = GetListField<byte>(comp, "blueWires").Get(lst => lst.Count > 7 ? "expected 7 or fewer elements" : null);
        var purpleWires = GetListField<byte>(comp, "purpleWires").Get(lst => lst.Count > 7 ? "expected 7 or fewer elements" : null);

        var totalWires = redWires.Count + orangeWires.Count + yellowWires.Count + greenWires.Count + blueWires.Count + purpleWires.Count;
        if (totalWires < 2 || totalWires > 7)
            throw new AbandonModuleException("All wires combined has unexpected value (expected 2-7): {1}", _moduleId, totalWires);

        var answerIndex = GetField<byte>(comp, "souvenir").Get(b => b < 2 || b > 8 ? "expected range 2–8" : null);
        addQuestions(module,
            makeQuestion(Question.RoleReversalWires, _RoleReversal, new[] { "warm-colored" }, correctAnswers: new[] { (redWires.Count + orangeWires.Count + yellowWires.Count).ToString() }),
            makeQuestion(Question.RoleReversalWires, _RoleReversal, new[] { "cold-colored" }, correctAnswers: new[] { (greenWires.Count + blueWires.Count + purpleWires.Count).ToString() }),
            makeQuestion(Question.RoleReversalWires, _RoleReversal, new[] { "primary-colored" }, correctAnswers: new[] { (redWires.Count + yellowWires.Count + blueWires.Count).ToString() }),
            makeQuestion(Question.RoleReversalWires, _RoleReversal, new[] { "secondary-colored" }, correctAnswers: new[] { (orangeWires.Count + greenWires.Count + purpleWires.Count).ToString() }),
            makeQuestion(Question.RoleReversalNumber, _RoleReversal, correctAnswers: new[] { answerIndex.ToString() }, preferredWrongAnswers: new[] { "2", "3", "4", "5", "6", "7", "8" }));
    }

    private IEnumerable<object> ProcessRule(KMBombModule module)
    {
        var comp = GetComponent(module, "TheRuleScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Rule);

        addQuestion(module, Question.RuleNumber, correctAnswers: new[] { GetIntField(comp, "ruleNumber").Get().ToString() });
    }

    private IEnumerable<object> ProcessScavengerHunt(KMBombModule module)
    {
        var comp = GetComponent(module, "scavengerHunt");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var keySquare = GetIntField(comp, "keySquare").Get(min: 0, max: 15);

        // Coordinates of the color that the user needed
        var relTiles = GetArrayField<int>(comp, "relTiles").Get(expectedLength: 2, validator: v => v < 0 || v > 15 ? "expected range 0–15" : null);

        // Coordinates of the other colors
        var decoyTiles = GetArrayField<int>(comp, "decoyTiles").Get(expectedLength: 4, validator: v => v < 4 || v > 15 ? "expected range 0–15" : null);

        // Which color is the ‘relTiles’ color
        var colorIndex = GetIntField(comp, "colorIndex").Get(min: 0, max: 2);

        // 0 = red, 1 = green, 2 = blue
        var redTiles = colorIndex == 0 ? relTiles : decoyTiles.Take(2).ToArray();
        var greenTiles = colorIndex == 1 ? relTiles : colorIndex == 0 ? decoyTiles.Take(2).ToArray() : decoyTiles.Skip(2).ToArray();
        var blueTiles = colorIndex == 2 ? relTiles : decoyTiles.Skip(2).ToArray();

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ScavengerHunt);

        addQuestions(module,
            makeQuestion(Question.ScavengerHuntKeySquare, _ScavengerHunt, correctAnswers: new[] { Tiles4x4Sprites[keySquare] }, preferredWrongAnswers: Tiles4x4Sprites),
            makeQuestion(Question.ScavengerHuntColoredTiles, _ScavengerHunt, formatArgs: new[] { "red" }, correctAnswers: redTiles.Select(c => Tiles4x4Sprites[c]).ToArray(), preferredWrongAnswers: Tiles4x4Sprites),
            makeQuestion(Question.ScavengerHuntColoredTiles, _ScavengerHunt, formatArgs: new[] { "green" }, correctAnswers: greenTiles.Select(c => Tiles4x4Sprites[c]).ToArray(), preferredWrongAnswers: Tiles4x4Sprites),
            makeQuestion(Question.ScavengerHuntColoredTiles, _ScavengerHunt, formatArgs: new[] { "blue" }, correctAnswers: blueTiles.Select(c => Tiles4x4Sprites[c]).ToArray(), preferredWrongAnswers: Tiles4x4Sprites));
    }

    private IEnumerable<object> ProcessSchlagDenBomb(KMBombModule module)
    {
        var comp = GetComponent(module, "qSchlagDenBomb");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SchlagDenBomb);

        var contestantName = GetField<string>(comp, "contestantName").Get();
        var contestantScore = GetIntField(comp, "scoreC").Get(min: 0, max: 75);
        var bombScore = GetIntField(comp, "scoreB").Get(min: 0, max: 75);

        addQuestions(module,
            makeQuestion(Question.SchlagDenBombContestantName, _SchlagDenBomb, correctAnswers: new[] { contestantName }),
            makeQuestion(Question.SchlagDenBombContestantScore, _SchlagDenBomb, correctAnswers: new[] { contestantScore.ToString() }, preferredWrongAnswers:
               Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(0, 75).ToString()).Distinct().Take(6).ToArray()),
            makeQuestion(Question.SchlagDenBombBombScore, _SchlagDenBomb, correctAnswers: new[] { bombScore.ToString() }, preferredWrongAnswers:
               Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(0, 75).ToString()).Distinct().Take(6).ToArray()));
    }

    private IEnumerable<object> ProcessSeaShells(KMBombModule module)
    {
        var comp = GetComponent(module, "SeaShellsModule");
        var fldRow = GetIntField(comp, "row");
        var fldCol = GetIntField(comp, "col");
        var fldKeynum = GetIntField(comp, "keynum");
        var fldStage = GetIntField(comp, "stage");
        var fldSolved = GetField<bool>(comp, "isPassed");
        var fldDisplay = GetField<TextMesh>(comp, "Display", isPublic: true);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var rows = new int[3];
        var cols = new int[3];
        var keynums = new int[3];
        while (true)
        {
            while (fldDisplay.Get().text == " ")
            {
                yield return new WaitForSeconds(.1f);
                if (fldSolved.Get())
                    goto solved;
            }

            var stage = fldStage.Get(min: 0, max: 2);
            rows[stage] = fldRow.Get();
            cols[stage] = fldCol.Get();
            keynums[stage] = fldKeynum.Get();

            while (fldDisplay.Get().text != " ")
            {
                yield return new WaitForSeconds(.1f);
                if (fldSolved.Get())
                    goto solved;
            }
        }

        solved:
        _modulesSolved.IncSafe(_SeaShells);

        var qs = new List<QandA>();
        for (int i = 0; i < 3; i++)
        {
            qs.Add(makeQuestion(Question.SeaShells1, _SeaShells, new[] { ordinal(i + 1) }, new[] { new[] { "she sells", "she shells", "sea shells", "sea sells" }[rows[i]] }));
            qs.Add(makeQuestion(Question.SeaShells2, _SeaShells, new[] { ordinal(i + 1) }, new[] { new[] { "sea shells", "she shells", "sea sells", "she sells" }[cols[i]] }));
            qs.Add(makeQuestion(Question.SeaShells3, _SeaShells, new[] { ordinal(i + 1) }, new[] { new[] { "sea shore", "she sore", "she sure", "seesaw" }[keynums[i]] }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSemamorse(KMBombModule module)
    {
        var comp = GetComponent(module, "semamorse");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Semamorse);

        var letters = GetArrayField<int[]>(comp, "displayedLetters").Get(expectedLength: 2, validator: arr => arr.Length != 5 ? "expected length 5" : arr.Any(v => v < 0 || v > 25) ? "expected range 0–25" : null);
        var colorNames = new string[] { "red", "green", "cyan", "indigo", "pink" };
        var colors = GetArrayField<int>(comp, "displayedColors").Get(expectedLength: 5, validator: c => c < 0 || c >= colorNames.Length ? string.Format("expected range 0–{0}", colorNames.Length - 1) : null);
        var qs = new List<QandA>();
        for (var dispIx = 0; dispIx < 5; dispIx++)
        {
            qs.Add(makeQuestion(Question.SemamorseColors, _Semamorse, formatArgs: new[] { ordinal(dispIx + 1) }, correctAnswers: new[] { colorNames[colors[dispIx]] }));
            qs.Add(makeQuestion(Question.SemamorseLetters, _Semamorse, formatArgs: new[] { ordinal(dispIx + 1), "Semaphore" }, correctAnswers: new[] { ((char) ('A' + letters[0][dispIx])).ToString() }));
            qs.Add(makeQuestion(Question.SemamorseLetters, _Semamorse, formatArgs: new[] { ordinal(dispIx + 1), "Morse" }, correctAnswers: new[] { ((char) ('A' + letters[1][dispIx])).ToString() }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessShapesAndBombs(KMBombModule module)
    {
        var comp = GetComponent(module, "ShapesBombs");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var initialLetter = GetIntField(comp, "selectLetter").Get(min: 0, max: 14);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ShapesBombs);

        var letterChars = new[] { "A", "B", "D", "E", "G", "I", "K", "L", "N", "O", "P", "S", "T", "X", "Y" };
        addQuestion(module, Question.ShapesAndBombsInitialLetter, correctAnswers: new[] { letterChars[initialLetter] });
    }

    private IEnumerable<object> ProcessShapeShift(KMBombModule module)
    {
        var comp = GetComponent(module, "ShapeShiftModule");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ShapeShift);

        var stL = GetIntField(comp, "startL").Get();
        var stR = GetIntField(comp, "startR").Get();
        var solL = GetIntField(comp, "solutionL").Get();
        var solR = GetIntField(comp, "solutionR").Get();
        var answers = new HashSet<string>();
        for (int l = 0; l < 4; l++)
            if (stL != solL || l == stL)
                for (int r = 0; r < 4; r++)
                    if (stR != solR || r == stR)
                        answers.Add(((char) ('A' + r + (4 * l))).ToString());
        if (answers.Count < 4)
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Shape Shift because the answer was the same as the initial state.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
        }
        else
            addQuestion(module, Question.ShapeShiftInitialShape, correctAnswers: new[] { ((char) ('A' + stR + (4 * stL))).ToString() }, preferredWrongAnswers: answers.ToArray());
    }

    private IEnumerable<object> ProcessShellGame(KMBombModule module)
    {
        var comp = GetComponent(module, "shellGame");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ShellGame);

        int initialCup = GetIntField(comp, "startingCup").Get(min: 0, max: 2);
        string[] position = new string[3] { "Left", "Middle", "Right" };
        addQuestions(module, makeQuestion(Question.ShellGameStartingCup, _ShellGame, correctAnswers: new[] { position[initialCup] }));
    }

    private IEnumerable<object> ProcessSillySlots(KMBombModule module)
    {
        var comp = GetComponent(module, "SillySlots");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_SillySlots);

        var prevSlots = GetField<IList>(comp, "mPreviousSlots").Get(lst => lst.Cast<object>().Any(obj => !(obj is Array) || ((Array) obj).Length != 3) ? "expected arrays of length 3" : null);
        if (prevSlots.Count < 2)
        {
            // Legitimate: first stage was a keep already
            Debug.LogFormat("[Souvenir #{0}] No question for Silly Slots because there was only one stage.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        var testSlot = ((Array) prevSlots[0]).GetValue(0);
        var fldShape = GetField<object>(testSlot, "shape", isPublic: true);
        var fldColor = GetField<object>(testSlot, "color", isPublic: true);

        var qs = new List<QandA>();
        // Skip the last stage because if the last action was Keep, it is still visible on the module
        for (int stage = 0; stage < prevSlots.Count - 1; stage++)
        {
            var slotStrings = ((Array) prevSlots[stage]).Cast<object>().Select(obj => (fldColor.GetFrom(obj).ToString() + " " + fldShape.GetFrom(obj).ToString()).ToLowerInvariant()).ToArray();
            for (int slot = 0; slot < slotStrings.Length; slot++)
                qs.Add(makeQuestion(Question.SillySlots, _SillySlots, new[] { ordinal(slot + 1), ordinal(stage + 1) }, new[] { slotStrings[slot] }, slotStrings));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonSamples(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSamples");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSamples);

        var calls = GetListField<string>(comp, "_calls").Get(expectedLength: 3);
        if (Enumerable.Range(1, 2).Any(i => calls[i].Length <= calls[i - 1].Length || !calls[i].StartsWith(calls[i - 1])))
            throw new AbandonModuleException("_calls=[{0}]; expected each element to start with the previous.", calls.Select(c => string.Format(@"""{0}""", c)).JoinString(", "));

        var formatArgs = new[] { "played in the first stage", "added in the second stage", "added in the third stage" };
        addQuestions(module, calls.Select((c, ix) => makeQuestion(Question.SimonSamplesSamples, _SimonSamples, new[] { formatArgs[ix] }, new[] { (ix == 0 ? c : c.Substring(calls[ix - 1].Length)).Replace("0", "K").Replace("1", "S").Replace("2", "H").Replace("3", "O") })));
    }

    private IEnumerable<object> ProcessSimonSays(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonComponent");
        var fldSolved = GetField<bool>(comp, "IsSolved", true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSays);

        var colorNames = new[] { "red", "blue", "green", "yellow" };
        var sequence = GetArrayField<int>(comp, "currentSequence").Get(validator: arr => arr.Any(i => i < 0 || i >= colorNames.Length) ? "expected values 0–3" : null);
        addQuestions(module, Enumerable.Range(0, sequence.Length).Select(i =>
            makeQuestion(Question.SimonSaysFlash, _SimonSays, formatArgs: new[] { ordinal(i + 1) }, correctAnswers: new[] { colorNames[sequence[i]] })));
    }

    private IEnumerable<object> ProcessSimonScrambles(KMBombModule module)
    {
        var comp = GetComponent(module, "simonScramblesScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonScrambles);

        int[] sequence = GetArrayField<int>(comp, "sequence").Get(expectedLength: 10);
        string[] colors = GetArrayField<string>(comp, "colorNames").Get(expectedLength: 4);

        if (sequence[9] < 0 || sequence[9] >= colors.Length)
            throw new AbandonModuleException("‘sequence[9]’ points to illegal color: {0} (expected 0-3).", sequence[9]);

        addQuestion(module, Question.SimonScramblesLastColor, correctAnswers: new[] { colors[sequence[9]] });
    }

    private IEnumerable<object> ProcessSimonScreams(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonScreamsModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_SimonScreams);

        var seqs = GetArrayField<int[]>(comp, "_sequences").Get(expectedLength: 3);
        var rules = GetField<Array>(comp, "_rowCriteria").Get(ar => ar.Length != 6 ? "expected length 6" : null);
        var colorsRaw = GetField<Array>(comp, "_colors").Get(ar => ar.Length != 3 ? "expected length 3" : null);     // array of enum values
        var colors = colorsRaw.Cast<object>().Select(obj => obj.ToString()).ToArray();

        var qs = new List<QandA>();
        var lastSeq = seqs.Last();
        for (int i = 0; i < lastSeq.Length; i++)
            qs.Add(makeQuestion(Question.SimonScreamsFlashing, _SimonScreams, new[] { ordinal(i + 1) }, new[] { colors[lastSeq[i]] }));

        // First determine which rule applied in which stage
        var fldCheck = GetField<Func<int[], bool>>(rules.GetValue(0), "Check", isPublic: true);
        var fldRuleName = GetField<string>(rules.GetValue(0), "Name", isPublic: true);
        var stageRules = new int[seqs.Length];
        for (int i = 0; i < seqs.Length; i++)
        {
            stageRules[i] = rules.Cast<object>().IndexOf(rule => fldCheck.GetFrom(rule)(seqs[i]));
            if (stageRules[i] == -1)
                throw new AbandonModuleException("Apparently none of the criteria applies to Stage {0} ({1}).", _moduleId, i + 1, seqs[i].Select(ix => colors[ix]).JoinString(", "));
        }

        // Now set the questions
        // Skip the last rule because it’s the “otherwise” row
        for (int rule = 0; rule < rules.Length - 1; rule++)
        {
            var applicableStages = new List<string>();
            for (int stage = 0; stage < stageRules.Length; stage++)
                if (stageRules[stage] == rule)
                    applicableStages.Add(ordinal(stage + 1));
            if (applicableStages.Count > 0)
                qs.Add(makeQuestion(Question.SimonScreamsRule, _SimonScreams,
                    new[] { fldRuleName.GetFrom(rules.GetValue(rule)) },
                    new[] { applicableStages.Count == stageRules.Length ? "all of them" : applicableStages.JoinString(", ", lastSeparator: " and ") },
                    applicableStages.Count == 1
                        ? Enumerable.Range(1, seqs.Length).Select(i => ordinal(i)).ToArray()
                        : Enumerable.Range(1, seqs.Length).SelectMany(a => Enumerable.Range(a + 1, seqs.Length - a).Select(b => ordinal(a) + " and " + ordinal(b))).Concat(new[] { "all of them" }).ToArray()));
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonSelects(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSelectsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSelects);

        var order = Enumerable.Range(0, 3).Select(i => GetArrayField<int>(comp, string.Format("stg{0}order", i + 1)).Get(minLength: 3, maxLength: 5)).ToArray();
        var btnRenderers = GetArrayField<Renderer>(comp, "buttonrend", isPublic: true).Get(expectedLength: 8);

        // Sequences of colors that flashes in each stage
        var seqs = new string[3][];

        // Parsing the received string
        for (int stage = 0; stage < 3; stage++)
        {
            var parsedString = new string[order[stage].Length];
            for (int flash = 0; flash < order[stage].Length; flash++)
                parsedString[flash] = btnRenderers[order[stage][flash]].material.name.Replace(" (Instance)", "");
            seqs[stage] = parsedString;
        }

        // Used to validate colors
        string[] colorNames = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Magenta", "Cyan" };

        if (seqs.Any(seq => seq.Any(color => !colorNames.Contains(color))))
            throw new AbandonModuleException("‘colors’ contains an invalid color: [{0}]", seqs.Select(seq => seq.JoinString(", ")).JoinString("; "));

        addQuestions(module, seqs.SelectMany((seq, stage) => seq.Select((col, ix) => makeQuestion(Question.SimonSelectsOrder, _SimonSelects,
            formatArgs: new[] { ordinal(ix + 1), ordinal(stage + 1) },
            correctAnswers: new[] { col }))));
    }

    private IEnumerable<object> ProcessSimonSends(KMBombModule module)
    {
        string[] morse = { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--.." };

        var comp = GetComponent(module, "SimonSendsModule");
        var morseR = GetField<string>(comp, "_morseR").Get();
        var morseG = GetField<string>(comp, "_morseG").Get();
        var morseB = GetField<string>(comp, "_morseB").Get();
        var charR = ((char) ('A' + Array.IndexOf(morse, morseR.Replace("###", "-").Replace("#", ".").Replace("_", "")))).ToString();
        var charG = ((char) ('A' + Array.IndexOf(morse, morseG.Replace("###", "-").Replace("#", ".").Replace("_", "")))).ToString();
        var charB = ((char) ('A' + Array.IndexOf(morse, morseB.Replace("###", "-").Replace("#", ".").Replace("_", "")))).ToString();

        if (charR == "@" || charG == "@" || charB == "@")
            throw new AbandonModuleException("Could not decode Morse code: {0} / {1} / {2}", morseR, morseG, morseB);

        // Simon Sends sets “_answerSoFar” to null when it’s done
        var fldAnswerSoFar = GetListField<int>(comp, "_answerSoFar");
        while (fldAnswerSoFar.Get(nullAllowed: true) != null)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_SimonSends);
        addQuestions(module,
            makeQuestion(Question.SimonSendsReceivedLetters, _SimonSends, new[] { "red" }, new[] { charR }, new[] { charG, charB }),
            makeQuestion(Question.SimonSendsReceivedLetters, _SimonSends, new[] { "green" }, new[] { charG }, new[] { charR, charB }),
            makeQuestion(Question.SimonSendsReceivedLetters, _SimonSends, new[] { "blue" }, new[] { charB }, new[] { charR, charG }));
    }

    private IEnumerable<object> ProcessSimonShrieks(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonShrieksModule");
        var fldStage = GetIntField(comp, "_stage");

        while (fldStage.Get() < 3)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonShrieks);

        var arrow = GetIntField(comp, "_arrow").Get(min: 0, max: 6);
        var flashingButtons = GetArrayField<int>(comp, "_flashingButtons").Get(expectedLength: 8, validator: b => b < 0 || b > 6 ? "expected range 0–6" : null);

        var qs = new List<QandA>();
        for (int i = 0; i < flashingButtons.Length; i++)
            qs.Add(makeQuestion(Question.SimonShrieksFlashingButton, _SimonShrieks, formatArgs: new[] { ordinal(i + 1) }, correctAnswers: new[] { ((flashingButtons[i] + 7 - arrow) % 7).ToString() }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonSimons(KMBombModule module)
    {
        var comp = GetComponent(module, "simonsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSimons);

        var flashes = new[] { "TR", "TY", "TG", "TB", "LR", "LY", "LG", "LB", "RR", "RY", "RG", "RB", "BR", "BY", "BG", "BB" };
        var buttonFlashes = GetArrayField<KMSelectable>(comp, "selButtons").Get(expectedLength: 5, validator: sel => !flashes.Contains(sel.name.ToUpperInvariant()) ? "invalid flash" : null);
        addQuestions(module, buttonFlashes.Select((btn, i) =>
            makeQuestion(Question.SimonSimonsFlashingColors, _SimonSimons, formatArgs: new[] { ordinal(i + 1) }, correctAnswers: new[] { btn.name.ToUpperInvariant() })));
    }

    private IEnumerable<object> ProcessSimonSings(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSingsModule");
        var fldCurStage = GetIntField(comp, "_curStage");

        while (fldCurStage.Get() < 3)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSings);

        var noteNames = new[] { "C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B" };
        var flashingColorSequences = GetArrayField<int[]>(comp, "_flashingColors").Get(expectedLength: 3, validator: seq => seq.Any(col => col < 0 || col >= noteNames.Length) ? string.Format("expected range 0–{0}", noteNames.Length - 1) : null);
        addQuestions(module, flashingColorSequences.SelectMany((seq, stage) => seq.Select((col, ix) => makeQuestion(Question.SimonSingsFlashing, _SimonSings, new[] { ordinal(ix + 1), ordinal(stage + 1) }, new[] { noteNames[col] }))));
    }

    private IEnumerable<object> ProcessSimonSounds(KMBombModule module)
    {
        var comp = GetComponent(module, "simonSoundsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSounds);

        var colorNames = new[] { "red", "blue", "yellow", "green" };
        var flashed = GetArrayField<List<int>>(comp, "stage").Get(ar => ar == null ? "contains null" : ar.Any(list => list.Last() < 0 || list.Last() >= colorNames.Length) ? "expected last value in range 0–3" : null);

        var qs = new List<QandA>();
        for (var stage = 0; stage < flashed.Length; stage++)
            qs.Add(makeQuestion(Question.SimonSoundsFlashingColors, _SimonSounds, formatArgs: new[] { ordinal(stage + 1) }, correctAnswers: new[] { colorNames[flashed[stage].Last()] }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonSpeaks(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSpeaksModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSpeaks);

        var sequence = GetArrayField<int>(comp, "_sequence").Get(expectedLength: 5);
        var colors = GetArrayField<int>(comp, "_colors").Get(expectedLength: 9);
        var words = GetArrayField<int>(comp, "_words").Get(expectedLength: 9);
        var languages = GetArrayField<int>(comp, "_languages").Get(expectedLength: 9);
        var wordsTable = GetStaticField<string[][]>(comp.GetType(), "_wordsTable").Get(ar => ar.Length != 9 ? "expected length 9" : null);
        var positionNames = GetStaticField<string[]>(comp.GetType(), "_positionNames").Get(ar => ar.Length != 9 ? "expected length 9" : null);

        addQuestions(module,
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.SimonSpeaksPositions, _SimonSpeaks, new[] { ordinal(ix + 1) }, new[] { positionNames[sequence[ix]] })).Concat(
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.SimonSpeaksColors, _SimonSpeaks, new[] { ordinal(ix + 1) }, new[] { wordsTable[colors[sequence[ix]]][0] })).Concat(
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.SimonSpeaksWords, _SimonSpeaks, new[] { ordinal(ix + 1) }, new[] { wordsTable[words[sequence[ix]]][languages[sequence[ix]]] })))));
    }

    private IEnumerable<object> ProcessSimonsStar(KMBombModule module)
    {
        var comp = GetComponent(module, "simonsStarScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var validColors = new[] { "red", "yellow", "green", "blue", "purple" };
        var flashes = "first,second,third,fourth,fifth".Split(',').Select(n => GetField<string>(comp, n + "FlashColour", isPublic: true).Get(c => !validColors.Contains(c) ? "invalid color" : null)).ToArray();

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonsStar);

        addQuestions(module, flashes.Select((f, ix) => makeQuestion(Question.SimonsStarColors, _SimonsStar, new[] { ordinal(ix + 1) }, new[] { f })));
    }

    private IEnumerable<object> ProcessSimonStates(KMBombModule module)
    {
        var comp = GetComponent(module, "AdvancedSimon");
        var fldPuzzleDisplay = GetArrayField<bool[]>(comp, "PuzzleDisplay");
        var fldProgress = GetIntField(comp, "Progress");

        bool[][] puzzleDisplay;
        while ((puzzleDisplay = fldPuzzleDisplay.Get(nullAllowed: true)) == null)
            yield return new WaitForSeconds(.1f);

        if (puzzleDisplay.Length != 4 || puzzleDisplay.Any(arr => arr.Length != 4))
            throw new AbandonModuleException("‘PuzzleDisplay’ has an unexpected length or value: [{0}]",
                puzzleDisplay.Select(arr => arr == null ? "null" : "[" + arr.JoinString(", ") + "]").JoinString("; "), _moduleId);

        var colorNames = new[] { "Red", "Yellow", "Green", "Blue" };

        while (fldProgress.Get() < 4)
            yield return new WaitForSeconds(.1f);
        // Consistency check
        if (fldPuzzleDisplay.Get(nullAllowed: true) != null)
            throw new AbandonModuleException("‘PuzzleDisplay’ was expected to be null when Progress reached 4, but wasn’t.", _moduleId);

        _modulesSolved.IncSafe(_SimonStates);

        var qs = new List<QandA>();
        for (int i = 0; i < 4; i++)
        {
            var c = puzzleDisplay[i].Count(b => b);
            if (c != 3)
                qs.Add(makeQuestion(Question.SimonStatesDisplay, _SimonStates,
                    new[] { "color(s) flashed", ordinal(i + 1) },
                    new[] { c == 4 ? "all 4" : puzzleDisplay[i].Select((v, j) => v ? colorNames[j] : null).Where(x => x != null).JoinString(", ") }));
            if (c != 1)
                qs.Add(makeQuestion(Question.SimonStatesDisplay, _SimonStates,
                    new[] { "color(s) didn’t flash", ordinal(i + 1) },
                    new[] { c == 4 ? "none" : puzzleDisplay[i].Select((v, j) => v ? null : colorNames[j]).Where(x => x != null).JoinString(", ") }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonStops(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonStops");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonStops);

        var colors = GetArrayField<string>(comp, "outputSequence").Get(expectedLength: 5);
        addQuestions(module, colors.Select((color, ix) =>
             makeQuestion(Question.SimonStopsColors, _SimonStops, new[] { ordinal(ix + 1) }, new[] { color }, colors)));
    }

    private IEnumerable<object> ProcessSimonStores(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonStoresScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_SimonStores);

        var flashSequences = GetListField<string>(comp, "flashingColours").Get();
        var colors = "RGBCMY";

        foreach (var flash in flashSequences)
        {
            var set = new HashSet<char>();
            if (flash.Length < 1 || flash.Length > 3 || flash.Any(color => !set.Add(color) || !colors.Contains(color)))
                throw new AbandonModuleException("'flashingColours' contains value with duplicated colors, invalid color, or unexpected length (expected: 1-3): [flash: {0}, length: {1}]", flash, flash.Length);
        }

        var correctAnswers = GetArrayField<int[]>(comp, "step").Get(expectedLength: 3, validator: answer => answer.Length != 6 ? "expected length 6" : null);

        int[] integerAnswers = new int[3];
        integerAnswers[0] = correctAnswers[0][3];
        integerAnswers[1] = correctAnswers[1][4];
        integerAnswers[2] = correctAnswers[2][5];

        if (integerAnswers.Any(answer => answer <= -365 || answer >= 365))
            throw new AbandonModuleException("‘step’ contains an invalid answer to a stage (expected -364 to 364): [{0}]", integerAnswers.JoinString(", "));

        var colorNames = new Dictionary<char, string> {
            { 'R', "Red" },
            { 'G', "Green" },
            { 'B', "Blue" },
            { 'C', "Cyan" },
            { 'M', "Magenta" },
            { 'Y', "Yellow" }
        };

        var qs = new List<QandA>();
        for (var i = 0; i < 5; i++)
            qs.Add(makeQuestion(Question.SimonStoresColors, _SimonStores,
                formatArgs: new[] { flashSequences[i].Length == 1 ? "flashed" : "was one of the colors flashed", ordinal(i + 1) },
                correctAnswers: flashSequences[i].Select(ch => colorNames[ch]).ToArray()));

        for (var i = 0; i < 3; i++)
            qs.Add(makeQuestion(Question.SimonStoresAnswers, _SimonStores,
                formatArgs: new[] { (i + 1).ToString() },
                correctAnswers: new[] { integerAnswers[i].ToString() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSkewedSlots(KMBombModule module)
    {
        var comp = GetComponent(module, "SkewedModule");
        var fldNumbers = GetArrayField<int>(comp, "Numbers");
        var fldModuleActivated = GetField<bool>(comp, "moduleActivated");
        var fldSolved = GetField<bool>(comp, "solved");

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
            originalNumbers.Add(fldNumbers.Get(expectedLength: 3, validator: n => n < 0 || n > 9 ? "expected range 0–9" : null).JoinString());

            // When the user presses anything, Skewed Slots sets moduleActivated to false while the slots are spinning.
            while (fldModuleActivated.Get())
                yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_SkewedSlots);
        addQuestion(module, Question.SkewedSlotsOriginalNumbers, correctAnswers: new[] { originalNumbers.Last() },
            preferredWrongAnswers: originalNumbers.Take(originalNumbers.Count - 1).ToArray());
    }

    private IEnumerable<object> ProcessSkyrim(KMBombModule module)
    {
        var comp = GetComponent(module, "skyrimScript");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!fldSolved.Get())
            // Usually we’d wait 0.1 seconds at a time, but in this case we need to know immediately so that we can hook the buttons
            yield return null;
        _modulesSolved.IncSafe(_Skyrim);

        foreach (var fieldName in new[] { "cycleUp", "cycleDown", "accept", "submit", "race", "weapon", "enemy", "city", "shout" })
        {
            var btn = GetField<KMSelectable>(comp, fieldName, isPublic: true).Get();
            btn.OnInteract = delegate
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
                btn.AddInteractionPunch(.5f);
                return false;
            };
        }

        var qs = new List<QandA>();
        var questions = new[] { Question.SkyrimRace, Question.SkyrimWeapon, Question.SkyrimEnemy, Question.SkyrimCity };
        var fieldNames = new[] { "race", "weapon", "enemy", "city" };
        var flds = fieldNames.Select(name => GetListField<Texture>(comp, name + "Images", isPublic: true)).ToArray();
        var fldsCorrect = new[] { "correctRace", "correctWeapon", "correctEnemy", "correctCity" }.Select(name => GetField<Texture>(comp, name)).ToArray();
        for (int i = 0; i < fieldNames.Length; i++)
        {
            var list = flds[i].Get(expectedLength: 3);
            var correct = fldsCorrect[i].Get();
            qs.Add(makeQuestion(questions[i], _Skyrim, correctAnswers: list.Except(new[] { correct }).Select(t => t.name.Replace("'", "’")).ToArray()));
        }
        var shoutNames = GetListField<string>(comp, "shoutNameOptions").Get(expectedLength: 3);
        qs.Add(makeQuestion(Question.SkyrimDragonShout, _Skyrim, correctAnswers: shoutNames.Except(new[] { GetField<string>(comp, "shoutName").Get() }).Select(n => n.Replace("'", "’")).ToArray()));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSnooker(KMBombModule module)
    {
        var comp = GetComponent(module, "snookerScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Snooker);

        addQuestion(module, Question.SnookerReds, correctAnswers: new[] { GetIntField(comp, "activeReds").Get(min: 8, max: 10).ToString() });
    }

    private IEnumerable<object> ProcessSonicTheHedgehog(KMBombModule module)
    {
        var comp = GetComponent(module, "sonicScript");
        var fldsButtonSounds = new[] { "boots", "invincible", "life", "rings" }.Select(name => GetField<string>(comp, name + "Press"));
        var fldsPics = Enumerable.Range(0, 3).Select(i => GetField<Texture>(comp, "pic" + (i + 1))).ToArray();
        var fldStage = GetIntField(comp, "stage");

        while (fldStage.Get() < 5)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SonicTheHedgehog);

        var soundNameMapping =
            @"boss=Boss Theme;breathe=Breathe;continueSFX=Continue;drown=Drown;emerald=Emerald;extraLife=Extra Life;finalZone=Final Zone;invincibleSFX=Invincibility;jump=Jump;lamppost=Lamppost;marbleZone=Marble Zone;bumper=Bumper;skid=Skid;spikes=Spikes;spin=Spin;spring=Spring"
                .Split(';').Select(str => str.Split('=')).ToDictionary(ar => ar[0], ar => ar[1]);
        var pictureNameMapping =
            @"annoyedSonic=Annoyed Sonic=2;ballhog=Ballhog=1;blueLamppost=Blue Lamppost=3;burrobot=Burrobot=1;buzzBomber=Buzz Bomber=1;crabMeat=Crab Meat=1;deadSonic=Dead Sonic=2;drownedSonic=Drowned Sonic=2;fallingSonic=Falling Sonic=2;motoBug=Moto Bug=1;redLamppost=Red Lamppost=3;redSpring=Red Spring=3;standingSonic=Standing Sonic=2;switch=Switch=3;yellowSpring=Yellow Spring=3"
                .Split(';').Select(str => str.Split('=')).ToDictionary(ar => ar[0], ar => new { Name = ar[1], Stage = int.Parse(ar[2]) - 1 });

        var pics = fldsPics.Select(f => f.Get(p => p.name == null || !pictureNameMapping.ContainsKey(p.name) ? "unknown pic" : null)).ToArray();
        var sounds = fldsButtonSounds.Select(f => f.Get(s => !soundNameMapping.ContainsKey(s) ? "unknown sound" : null)).ToArray();

        addQuestions(module,
            Enumerable.Range(0, 3).Select(i =>
                makeQuestion(
                    Question.SonicTheHedgehogPictures,
                    _SonicTheHedgehog,
                    new[] { ordinal(i + 1) },
                    new[] { pictureNameMapping[pics[i].name].Name },
                    pictureNameMapping.Values.Where(inf => inf.Stage == i).Select(inf => inf.Name).ToArray()))
            .Concat(new[] { "Running Boots", "Invincibility", "Extra Life", "Rings" }.Select((screenName, i) =>
                makeQuestion(
                    Question.SonicTheHedgehogSounds,
                    _SonicTheHedgehog,
                    new[] { screenName },
                    new[] { soundNameMapping[sounds[i]] },
                    sounds.Select(s => soundNameMapping[s]).ToArray()))));
    }

    private IEnumerable<object> ProcessSorting(KMBombModule module)
    {
        var comp = GetComponent(module, "Sorting");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Sorting);

        var lastSwap = GetField<byte>(comp, "swapButtons").Get();
        if (lastSwap % 10 == 0 || lastSwap % 10 > 5 || lastSwap / 10 == 0 || lastSwap / 10 > 5 || lastSwap / 10 == lastSwap % 10)
            throw new AbandonModuleException("‘swap’ has unexpected value (expected two digit number, each with a unique digit from 1-5): {0}", lastSwap);

        addQuestions(module, makeQuestion(Question.SortingLastSwap, _Sorting, correctAnswers: new[] { lastSwap.ToString().Insert(1, " & ") }));
    }

    private IEnumerable<object> ProcessSouvenir(KMBombModule module)
    {
        var comp = module.GetComponent<SouvenirModule>();
        if (comp == this)
        {
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        int souvenirCount;
        if (!_moduleCounts.TryGetValue(_Souvenir, out souvenirCount) || souvenirCount != 2)
        {
            if (souvenirCount > 2)
                Debug.LogFormat("[Souvenir #{0}] There are more than two Souvenir modules on this bomb. Not asking any questions about them.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        // Prefer names of supported modules on the bomb other than Souvenir.
        IEnumerable<string> modules = supportedModuleNames.Except(new[] { "Souvenir" });
        if (supportedModuleNames.Count < 5)
        {
            // If there are less than 4 eligible modules, fill the remaining spaces with random other modules.
            var allModules = _attributes.Where(x => x.Value != null).Select(x => x.Value.ModuleNameWithThe).Distinct().ToList();
            modules = modules.Concat(Enumerable.Range(0, 1000).Select(i => allModules[Rnd.Range(0, allModules.Count)]).Except(supportedModuleNames).Take(5 - supportedModuleNames.Count));
        }
        while (comp._currentQuestion == null)
            yield return new WaitForSeconds(0.1f);

        var firstQuestion = comp._currentQuestion;
        var firstModule = firstQuestion.ModuleNameWithThe;

        // Wait for the user to solve that question before asking about it
        while (comp._currentQuestion == firstQuestion)
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_Souvenir);
        addQuestion(module, Question.SouvenirFirstQuestion, null, new[] { firstModule }, modules.ToArray());
    }

    private IEnumerable<object> ProcessSphere(KMBombModule module)
    {
        var comp = GetComponent(module, "theSphereScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        string[] colorNames = GetArrayField<string>(comp, "colourNames", isPublic: true).Get();
        int[] colors = GetArrayField<int>(comp, "selectedColourIndices", isPublic: true).Get(expectedLength: 5, validator: c => c < 0 || c >= colorNames.Length ? string.Format("expected range 0–{0}", colorNames.Length - 1) : null);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Sphere);
        addQuestions(module,
            makeQuestion(Question.SphereColors, _Sphere, new[] { "first" }, new[] { colorNames[colors[0]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "second" }, new[] { colorNames[colors[1]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "third" }, new[] { colorNames[colors[2]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "fourth" }, new[] { colorNames[colors[3]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "fifth" }, new[] { colorNames[colors[4]] }));
    }

    private IEnumerable<object> ProcessSplittingTheLoot(KMBombModule module)
    {
        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var comp = GetComponent(module, "SplittingTheLootScript");
        var bags = GetField<IList>(comp, "bags").Get(lst => lst.Count != 7 ? "expected length 7" : null);
        var fldBagColor = GetField<object>(bags[0], "Color");
        var fldBagLabel = GetField<string>(bags[0], "Label");
        var bagColors = bags.Cast<object>().Select(obj => fldBagColor.GetFrom(obj)).ToArray();
        var bagLabels = bags.Cast<object>().Select(obj => fldBagLabel.GetFrom(obj)).ToArray();
        var paintedBag = bagColors.IndexOf(bc => bc.ToString() != "Normal");
        if (paintedBag == -1)
            throw new AbandonModuleException("No colored bag was found: [{0}]", bagColors.JoinString(", "));

        var fldSolved = GetField<bool>(comp, "isSolved");
        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SplittingTheLoot);

        addQuestion(module, Question.SplittingTheLootColoredBag, correctAnswers: new[] { bagLabels[paintedBag] }, preferredWrongAnswers: bagLabels);
    }

    private IEnumerable<object> ProcessSwitch(KMBombModule module)
    {
        var comp = GetComponent(module, "Switch");
        var fldSolved = GetField<bool>(comp, "SOLVED");
        var fldBottomColor = GetIntField(comp, "BottomColor");
        var fldTopColor = GetIntField(comp, "TopColor");
        var fldFirstSuccess = GetField<bool>(comp, "FirstSuccess");

        var colorNames = new[] { "red", "orange", "yellow", "green", "blue", "purple" };

        var topColor1 = fldTopColor.Get();
        var bottomColor1 = fldBottomColor.Get();
        var topColor2 = -1;
        var bottomColor2 = -1;

        var switchSelectable = GetField<KMSelectable>(comp, "FlipperSelectable", isPublic: true).Get();

        var prevInteract = switchSelectable.OnInteract;
        switchSelectable.OnInteract = delegate
        {
            var ret = prevInteract();

            // Only access bool and int fields in this button handler, so no exceptions are thrown
            var firstSuccess = fldFirstSuccess.Get();
            if (!firstSuccess)  // This means the user got a strike. Need to retrieve the new colors
            {
                topColor1 = fldTopColor.Get();
                bottomColor1 = fldBottomColor.Get();
            }
            else if (!fldSolved.Get())
            {
                topColor2 = fldTopColor.Get();
                bottomColor2 = fldBottomColor.Get();
            }
            return ret;
        };

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Switch);

        if (topColor1 < 1 || topColor1 > 6 || bottomColor1 < 1 || bottomColor1 > 6 || topColor2 < 1 || topColor2 > 6 || bottomColor2 < 1 || bottomColor2 > 6)
            throw new AbandonModuleException("topColor1/bottomColor1/topColor2/bottomColor2 have unexpected values: {1}, {2}, {3}, {4} (expected 1–6).", _moduleId, topColor1, bottomColor1, topColor2, bottomColor2);

        addQuestions(module,
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "top", "first" }, new[] { colorNames[topColor1 - 1] }),
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "bottom", "first" }, new[] { colorNames[bottomColor1 - 1] }),
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "top", "second" }, new[] { colorNames[topColor2 - 1] }),
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "bottom", "second" }, new[] { colorNames[bottomColor2 - 1] }));
    }

    private IEnumerable<object> ProcessSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "SwitchModule");
        var fldGoal = GetField<object>(comp, "_goalConfiguration");
        var mthCurConfig = GetMethod<object>(comp, "GetCurrentConfiguration", 0);
        var switches = GetArrayField<MonoBehaviour>(comp, "Switches", isPublic: true).Get(expectedLength: 5);

        // The special font Souvenir uses to display switch states uses Q for up and R for down
        var initialState = switches.Select(sw => sw.GetComponent<Animator>().GetBool("Up") ? "Q" : "R").JoinString();

        while (!fldGoal.Get().Equals(mthCurConfig.Invoke()))
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Switches);

        addQuestion(module, Question.SwitchesInitialPosition, correctAnswers: new[] { initialState });
    }

    private IEnumerable<object> ProcessSymbolCycle(KMBombModule module)
    {
        var comp = GetComponent(module, "SymbolCycleModule");
        var fldCycles = GetArrayField<int[]>(comp, "_cycles");
        var fldState = GetField<object>(comp, "_state");

        int[][] cycles = null;
        while (fldState.Get().ToString() != "Solved")
        {
            cycles = fldCycles.Get(expectedLength: 2, validator: x => x.Length < 2 || x.Length > 5 ? "expected length 2–5" : null);

            while (fldState.Get().ToString() == "Cycling")
                yield return new WaitForSeconds(0.1f);

            while (fldState.Get().ToString() == "Retrotransphasic" || fldState.Get().ToString() == "Anterodiametric")
                yield return new WaitForSeconds(0.1f);
        }

        if (cycles == null)
            throw new AbandonModuleException("No cycles.");

        _modulesSolved.IncSafe(_SymbolCycle);
        addQuestions(module, new[] { "left", "right" }.Select((screen, ix) => makeQuestion(Question.SymbolCycleSymbolCounts, _SymbolCycle, new[] { screen }, new[] { cycles[ix].Length.ToString() })));
    }

    private IEnumerable<object> ProcessSymbolicCoordinates(KMBombModule module)
    {
        var comp = GetComponent(module, "symbolicCoordinatesScript");
        var letter1 = GetField<string>(comp, "letter1").Get();
        var letter2 = GetField<string>(comp, "letter2").Get();
        var letter3 = GetField<string>(comp, "letter3").Get();

        var stageLetters = new[] { letter1.Split(' '), letter2.Split(' '), letter3.Split(' ') };

        if (stageLetters.Any(x => x.Length != 3) || stageLetters.SelectMany(x => x).Any(y => !"ACELP".Contains(y)))
            throw new AbandonModuleException("One of the stages has fewer than 3 symbols or symbols are of unexpected value (expected symbols “ACELP”, got “{0}”).", stageLetters.Select(x => string.Format("“{0}”", x.JoinString())).JoinString(", "));

        var fldStage = GetIntField(comp, "stage");
        while (fldStage.Get() < 4)
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_SymbolicCoordinates);

        var position = new[] { "left", "middle", "right" };
        addQuestions(module, stageLetters.SelectMany((letters, stage) => letters.Select((letter, pos) => makeQuestion(
            Question.SymbolicCoordinateSymbols,
            _SymbolicCoordinates,
            formatArgs: new[] { position[pos], ordinal(stage + 1) },
            correctAnswers: new[] { SymbolicCoordinatesSprites["ACELP".IndexOf(letter, StringComparison.Ordinal)] },
            preferredWrongAnswers: SymbolicCoordinatesSprites))));
    }

    private IEnumerable<object> ProcessSynonyms(KMBombModule module)
    {
        var comp = GetComponent(module, "Synonyms");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var numberText = GetField<TextMesh>(comp, "NumberText", isPublic: true).Get();

        int number;
        if (numberText.text == null || !int.TryParse(numberText.text, out number) || number < 0 || number > 9)
            throw new AbandonModuleException("The display text (“{0}”) is not an integer 0–9.", numberText.text ?? "<null>");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Synonyms);

        numberText.gameObject.SetActive(false);
        GetField<TextMesh>(comp, "BadLabel", isPublic: true).Get().text = "INPUT";
        GetField<TextMesh>(comp, "GoodLabel", isPublic: true).Get().text = "ACCEPTED";

        addQuestion(module, Question.SynonymsNumber, correctAnswers: new[] { number.ToString() });
    }

    private IEnumerable<object> ProcessTapCode(KMBombModule module)
    {
        var comp = GetComponent(module, "TapCodeScript");
        var fldSolved = GetField<bool>(comp, "modulepass");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_TapCode);

        var words = GetArrayField<string>(comp, "words").Get();
        var chosenWord = GetField<string>(comp, "chosenWord").Get(str => !words.Contains(str) ? string.Format("word is not in list: {0}", words.JoinString(", ")) : null);
        addQuestion(module, Question.TapCodeReceivedWord, correctAnswers: new[] { chosenWord }, preferredWrongAnswers: words);
    }

    private IEnumerable<object> ProcessTashaSqueals(KMBombModule module)
    {
        var comp = GetComponent(module, "tashaSquealsScript");
        var fldSolved = GetField<bool>(comp, "solved");

        var colors = GetStaticField<string[]>(comp.GetType(), "colorNames").Get(ar => ar.Length != 4 ? "expected length 4" : null).ToArray();
        var sequence = GetArrayField<int>(comp, "flashing").Get(expectedLength: 5, validator: val => val < 0 || val >= colors.Length ? string.Format("expected range 0–{0}", colors.Length - 1) : null);

        for (int i = 0; i < colors.Length; i++)
            colors[i] = char.ToUpperInvariant(colors[i][0]) + colors[i].Substring(1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_TashaSqueals);
        addQuestions(module,
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "first" }, new[] { colors[sequence[0]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "second" }, new[] { colors[sequence[1]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "third" }, new[] { colors[sequence[2]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "fourth" }, new[] { colors[sequence[3]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "fifth" }, new[] { colors[sequence[4]] }));
    }

    private IEnumerable<object> ProcessTenButtonColorCode(KMBombModule module)
    {
        var comp = GetComponent(module, "scr_colorCode");
        var fldSolvedFirstStage = GetField<bool>(comp, "solvedFirst");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldColors = GetArrayField<int>(comp, "prevColors");

        // Take a copy because the module modifies the same array in the second stage
        var firstStageColors = fldColors.Get(expectedLength: 10).ToArray();

        while (!fldSolvedFirstStage.Get())
            yield return new WaitForSeconds(.1f);

        var secondStageColors = fldColors.Get(expectedLength: 10);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_TenButtonColorCode);

        var colorNames = new[] { "red", "green", "blue" };
        addQuestions(module, new[] { firstStageColors, secondStageColors }.SelectMany((colors, stage) => Enumerable.Range(0, 10)
            .Select(slot => makeQuestion(Question.TenButtonColorCodeInitialColors, _TenButtonColorCode, new[] { ordinal(slot + 1), ordinal(stage + 1) }, new[] { colorNames[colors[slot]] }))));
    }

    private IEnumerable<object> ProcessTextField(KMBombModule module)
    {
        var comp = GetComponent(module, "TextField");

        var fldActivated = GetField<bool>(comp, "_lightson");
        while (!fldActivated.Get())
            yield return new WaitForSeconds(0.1f);

        var displayMeshes = GetArrayField<TextMesh>(comp, "ButtonLabels", true).Get(expectedLength: 12, validator: tm => tm.text == null ? "text is null" : null);
        var answer = displayMeshes.Select(x => x.text).FirstOrDefault(x => x != "✓" && x != "✗");
        var possibleAnswers = new[] { "A", "B", "C", "D", "E", "F" };

        if (!possibleAnswers.Contains(answer))
            throw new AbandonModuleException("Answer ‘{0}’ is not of expected value ({1}).", answer ?? "<null>", possibleAnswers.JoinString(", "));

        var fldSolved = GetField<bool>(comp, "_isSolved");
        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        for (var i = 0; i < 12; i++)
            if (displayMeshes[i].text == answer)
                displayMeshes[i].text = "✓";

        _modulesSolved.IncSafe(_TextField);
        addQuestion(module, Question.TextFieldDisplay, correctAnswers: new[] { answer });
    }

    private IEnumerable<object> ProcessThinkingWires(KMBombModule module)
    {
        var comp = GetComponent(module, "thinkingWiresScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_ThinkingWires);

        var validWires = new[] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White", "Black", "Any" };
        var firstCorrectWire = GetIntField(comp, "firstWireToCut").Get(min: 1, max: 7);
        var secondCorrectWire = GetField<string>(comp, "secondWireToCut").Get(str => !validWires.Contains(str) ? string.Format("invalid color; expected: {0}", validWires.JoinString(", ")) : null);
        var displayNumber = GetField<string>(comp, "screenNumber").Get();

        // List of valid display numbers for validation. 69 happens in the case of "Any" while 11 is expected to be the longest.
        // Basic calculations by hand and algorithm seem to confirm this, but may want to recalculate to ensure it is right.
        if (!new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "69" }.Contains(displayNumber))
            throw new AbandonModuleException("‘displayNumber’ has an unexpected value: {0}", displayNumber);

        addQuestions(module,
            makeQuestion(Question.ThinkingWiresFirstWire, _ThinkingWires, null, new[] { firstCorrectWire.ToString() }),
            makeQuestion(Question.ThinkingWiresSecondWire, _ThinkingWires, null, new[] { secondCorrectWire }),
            makeQuestion(Question.ThinkingWiresDisplayNumber, _ThinkingWires, null, new[] { displayNumber }));
    }

    private IEnumerable<object> ProcessThirdBase(KMBombModule module)
    {
        var comp = GetComponent(module, "ThirdBaseModule");
        var fldStage = GetIntField(comp, "stage");
        var fldActivated = GetField<bool>(comp, "isActivated");
        var fldSolved = GetField<bool>(comp, "isPassed");
        var displayTextMesh = GetField<TextMesh>(comp, "Display", isPublic: true).Get();

        while (!fldActivated.Get())
            yield return new WaitForSeconds(0.1f);

        var displayWords = new string[2];

        for (var i = 0; i < 2; i++)
            while (fldStage.Get() == i)
            {
                while (!fldActivated.Get())
                    yield return new WaitForSeconds(0.1f);

                displayWords[i] = displayTextMesh.text;

                while (fldActivated.Get())
                    yield return new WaitForSeconds(0.1f);
            }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_ThirdBase);
        addQuestions(module, displayWords.Select((word, stage) => makeQuestion(Question.ThirdBaseDisplay, _ThirdBase, new[] { ordinal(stage + 1) }, new[] { word })));
    }

    private IEnumerable<object> ProcessTicTacToe(KMBombModule module)
    {
        var comp = GetComponent(module, "TicTacToeModule");
        var fldIsInitialized = GetField<bool>(comp, "_isInitialized");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");

        while (!fldIsInitialized.Get())
            yield return new WaitForSeconds(.1f);

        var keypadButtons = GetArrayField<KMSelectable>(comp, "KeypadButtons", isPublic: true).Get(expectedLength: 9);
        var keypadPhysical = GetArrayField<KMSelectable>(comp, "_keypadButtonsPhysical").Get(expectedLength: 9);

        // Take a copy of the placedX array because it changes
        var placedX = GetArrayField<bool?>(comp, "_placedX").Get(expectedLength: 9).ToArray();

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_TicTacToe);

        var buttonNames = new[] { "top-left", "top-middle", "top-right", "middle-left", "middle-center", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };
        addQuestions(module, Enumerable.Range(0, 9).Select(ix => makeQuestion(Question.TicTacToeInitialState, _TicTacToe,
            formatArgs: new[] { buttonNames[Array.IndexOf(keypadPhysical, keypadButtons[ix])] },
            correctAnswers: new[] { placedX[ix] == null ? (ix + 1).ToString() : placedX[ix].Value ? "X" : "O" })));
    }

    private IEnumerable<object> ProcessTimezone(KMBombModule module)
    {
        var comp = GetComponent(module, "TimezoneScript");
        var fldFromCity = GetField<string>(comp, "from");
        var fldToCity = GetField<string>(comp, "to");
        var textFromCity = GetField<TextMesh>(comp, "TextFromCity", isPublic: true).Get();
        var textToCity = GetField<TextMesh>(comp, "TextToCity", isPublic: true).Get();

        if (fldFromCity.Get() != textFromCity.text || fldToCity.Get() != textToCity.text)
            throw new AbandonModuleException("The city names don’t match up: “{0}” vs. “{1}” and “{2}” vs. “{3}”.", fldFromCity.Get(), textFromCity.text, fldToCity.Get(), textToCity.text);

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Timezone);

        textFromCity.text = "WELL";
        textToCity.text = "DONE!";
        addQuestions(module,
            makeQuestion(Question.TimezoneCities, _Timezone, new[] { "departure" }, new[] { fldFromCity.Get() }),
            makeQuestion(Question.TimezoneCities, _Timezone, new[] { "destination" }, new[] { fldToCity.Get() }));
    }

    private IEnumerable<object> ProcessTransmittedMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "TransmittedMorseScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldMessage = GetField<string>(comp, "messagetrans");
        var fldStage = GetIntField(comp, "stage");

        string[] messages = new string[2];
        int stage = 0;

        while (!fldSolved.Get())
        {
            stage = fldStage.Get(min: 1, max: 2);
            messages[stage - 1] = fldMessage.Get();
            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_TransmittedMorse);

        addQuestions(module, messages.Select((msg, index) => makeQuestion(Question.TransmittedMorseMessage, _TransmittedMorse,
            formatArgs: new[] { ordinal(index + 1) },
            correctAnswers: new[] { msg },
            preferredWrongAnswers: messages)));
    }

    private IEnumerable<object> ProcessTurtleRobot(KMBombModule module)
    {
        var comp = GetComponent(module, "TurtleRobot");
        var fldCursor = GetIntField(comp, "_cursor");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var mthFormatCommand = GetMethod<string>(comp, "FormatCommand", 2);
        var commands = GetField<IList>(comp, "_commands").Get();
        var deleteButton = GetField<KMSelectable>(comp, "ButtonDelete", isPublic: true).Get();

        var codeLines = commands.Cast<object>().Select(cmd => mthFormatCommand.Invoke(cmd, false)).ToArray();
        var bugs = new List<string>();
        var bugsMarked = new HashSet<int>();

        var buttonHandler = deleteButton.OnInteract;
        deleteButton.OnInteract = delegate
        {
            var ret = buttonHandler();
            var cursor = fldCursor.Get();   // int field: avoid throwing exceptions inside of the button handler
            var command = mthFormatCommand.Invoke(commands[cursor], true);
            if (command.StartsWith("#") && bugsMarked.Add(cursor))
                bugs.Add(codeLines[cursor]);
            return ret;
        };

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_TurtleRobot);
        addQuestions(module, bugs.Take(2).Select((bug, ix) => makeQuestion(Question.TurtleRobotCodeLines, _TurtleRobot, new[] { ordinal(ix + 1) }, new[] { bug }, codeLines)));
    }

    private IEnumerable<object> ProcessTwoBits(KMBombModule module)
    {
        var comp = GetComponent(module, "TwoBitsModule");

        var fldCurrentState = GetField<object>(comp, "currentState");
        while (fldCurrentState.Get().ToString() != "Complete")
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_TwoBits);

        var queryLookups = GetField<Dictionary<int, string>>(comp, "queryLookups").Get();
        var queryResponses = GetField<Dictionary<string, int>>(comp, "queryResponses").Get();

        var zerothNumCode = GetIntField(comp, "firstQueryCode").Get();
        var zerothLetterCode = queryLookups[zerothNumCode];
        var firstResponse = queryResponses[zerothLetterCode];
        var firstLookup = queryLookups[firstResponse];
        var secondResponse = queryResponses[firstLookup];
        var secondLookup = queryLookups[secondResponse];
        var thirdResponse = queryResponses[secondLookup];
        var preferredWrongAnswers = new[] { zerothNumCode.ToString("00"), firstResponse.ToString("00"), secondResponse.ToString("00"), thirdResponse.ToString("00") };

        addQuestions(module,
            makeQuestion(Question.TwoBitsResponse, _TwoBits, new[] { "first" }, new[] { firstResponse.ToString("00") }, preferredWrongAnswers),
            makeQuestion(Question.TwoBitsResponse, _TwoBits, new[] { "second" }, new[] { secondResponse.ToString("00") }, preferredWrongAnswers),
            makeQuestion(Question.TwoBitsResponse, _TwoBits, new[] { "third" }, new[] { thirdResponse.ToString("00") }, preferredWrongAnswers));
    }

    private IEnumerable<object> ProcessUltimateCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "UltimateCycleScript", Question.UltimateCycleWord, _UltimateCycle);
    }

    private IEnumerable<object> ProcessUltracube(KMBombModule module)
    {
        return processHypercubeUltracube(module, "TheUltracubeModule", Question.UltracubeRotations, _Ultracube);
    }

    private IEnumerable<object> ProcessUncoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "UncoloredSquaresModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_UncoloredSquares);
        addQuestions(module,
            makeQuestion(Question.UncoloredSquaresFirstStage, _UncoloredSquares, new[] { "first" }, new[] { GetField<object>(comp, "_firstStageColor1").Get().ToString() }),
            makeQuestion(Question.UncoloredSquaresFirstStage, _UncoloredSquares, new[] { "second" }, new[] { GetField<object>(comp, "_firstStageColor2").Get().ToString() }));
    }

    private IEnumerable<object> ProcessUncoloredSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "UncoloredSwitches");
        var fldInitialState = GetField<StringBuilder>(comp, "Switches_Current_State");
        var fldLedColors = GetField<StringBuilder>(comp, "LEDsColorsString");
        var fldStage = GetIntField(comp, "stage");

        var curStage = -1;
        var ledColors = new int[3][];
        var switchStates = new bool[3][];
        var colorNames = new[] { "red", "green", "blue", "turquoise", "orange", "purple", "white", "black" };
        while (fldStage.Get() < 4)
        {
            var newStage = fldStage.Get(st => st == 3 ? "didn’t expect 3" : st < 0 || st > 4 ? "expected 0–2 or 4" : null);
            if (newStage != curStage)
            {
                var curLedColors = fldLedColors.Get(str => str.Length != 10 ? "expected length 10" : null);
                var switchState = fldInitialState.Get(str => str.Length != 5 ? "expected length 5" : null);
                ledColors[newStage] = Enumerable.Range(0, 10).Select(ledIx => "RGBTOPWK".IndexOf(curLedColors[ledIx])).ToArray();
                switchStates[newStage] = Enumerable.Range(0, 5).Select(swIx => switchState[swIx] == '1').ToArray();
            }
            yield return null;
        }
        _modulesSolved.IncSafe(_UncoloredSwitches);

        if (ledColors.Contains(null) || switchStates.Contains(null))
            throw new AbandonModuleException("A stage was skipped: [{0}], [{1}]",
                ledColors.Select(arr => arr == null ? "null" : string.Format(@"[{0}]", arr.JoinString(", "))),
                switchStates.Select(arr => arr == null ? "null" : string.Format(@"[{0}]", arr.JoinString(", "))));

        var qs = new List<QandA>();
        for (var stage = 0; stage < 3; stage++)
        {
            qs.Add(makeQuestion(Question.UncoloredSwitchesInitialState, _UncoloredSwitches, formatArgs: new[] { ordinal(stage + 1) }, correctAnswers: new[] { switchStates[stage].Select(b => b ? 'Q' : 'R').JoinString() }));
            for (var ledIx = 0; ledIx < 10; ledIx++)
                qs.Add(makeQuestion(Question.UncoloredSwitchesLedColors, _UncoloredSwitches, formatArgs: new[] { ordinal(ledIx + 1), ordinal(stage + 1) }, correctAnswers: new[] { colorNames[ledColors[stage][ledIx]] }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessUnfairCipher(KMBombModule module)
    {
        var comp = GetComponent(module, "unfairCipherScript");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_UnfairCipher);

        var instructions = GetArrayField<string>(comp, "Message").Get(expectedLength: 4);
        addQuestions(module,
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "first" }, new[] { instructions[0] }),
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "second" }, new[] { instructions[1] }),
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "third" }, new[] { instructions[2] }),
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "fourth" }, new[] { instructions[3] }));
    }

    private IEnumerable<object> ProcessUnownCipher(KMBombModule module)
    {
        var comp = GetComponent(module, "UnownCipher");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_UnownCipher);

        var unownAnswer = GetArrayField<int>(comp, "letterIndexes").Get(expectedLength: 5, validator: v => v < 0 || v > 25 ? "expected 0–25" : null);
        addQuestions(module, unownAnswer.Select((ans, i) => makeQuestion(Question.UnownCipherAnswers, _UnownCipher, new[] { ordinal(i + 1) }, new[] { ((char) ('A' + ans)).ToString() })));
    }

    private IEnumerable<object> ProcessUSAMaze(KMBombModule module)
    {
        return processWorldMaze(module, "USAMaze", _USAMaze, Question.USAMazeOrigin);
    }

    private IEnumerable<object> ProcessVaricoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "VaricoloredSquaresModule");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_VaricoloredSquares);

        addQuestion(module, Question.VaricoloredSquaresInitialColor, correctAnswers: new[] { GetField<object>(comp, "_firstStageColor").Get().ToString() });
    }

    private IEnumerable<object> ProcessVcrcs(KMBombModule module)
    {
        var comp = GetComponent(module, "VcrcsScript");
        var fldSolved = GetField<bool>(comp, "ModuleSolved");

        var wordTextMesh = GetField<TextMesh>(comp, "Words", isPublic: true).Get();
        var word = wordTextMesh.text;
        if (word == null)
            throw new AbandonModuleException("‘Words.text’ is null.");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Vcrcs);

        addQuestion(module, Question.VcrcsWord, correctAnswers: new[] { word });
    }

    private IEnumerable<object> ProcessVectors(KMBombModule module)
    {
        var comp = GetComponent(module, "VectorsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Vectors);

        var colorsName = new[] { "Red", "Orange", "Yellow", "Green", "Blue", "Purple" };
        var vectorCount = GetIntField(comp, "vectorct").Get(min: 1, max: 3);
        var colors = GetArrayField<string>(comp, "colors").Get();
        var pickedVectors = GetArrayField<int>(comp, "vectorsPicked").Get(expectedLength: 3, validator: v => v < 0 || v >= colors.Length ? string.Format("expected range 0–{0}", colors.Length - 1) : null);

        for (int i = 0; i < vectorCount; i++)
            if (!colorsName.Contains(colors[pickedVectors[i]]))
                throw new AbandonModuleException("‘colors[{1}]’ pointed to illegal color “{2}” (colors=[{3}], pickedVectors=[{4}], index {0}).",
                    i, pickedVectors[i], colors[pickedVectors[i]], colors.JoinString(", "), pickedVectors.JoinString(", "));

        var qs = new List<QandA>();
        for (int i = 0; i < vectorCount; i++)
            qs.Add(makeQuestion(Question.VectorsColors, _Vectors, new[] { vectorCount == 1 ? "only" : ordinal(i + 1) }, new[] { colors[pickedVectors[i]] }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessVexillology(KMBombModule module)
    {
        var comp = GetComponent(module, "vexillologyScript");
        var fldSolved = GetField<bool>(comp, "_issolved");

        string[] colors = GetArrayField<string>(comp, "coloursStrings").Get();
        int color1 = GetIntField(comp, "ActiveFlagTop1").Get(min: 0, max: colors.Length - 1);
        int color2 = GetIntField(comp, "ActiveFlagTop2").Get(min: 0, max: colors.Length - 1);
        int color3 = GetIntField(comp, "ActiveFlagTop3").Get(min: 0, max: colors.Length - 1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Vexillology);

        addQuestions(module,
            makeQuestion(Question.VexillologyColors, _Vexillology, new[] { "first" }, new[] { colors[color1] }, new[] { colors[color2], colors[color3] }),
            makeQuestion(Question.VexillologyColors, _Vexillology, new[] { "second" }, new[] { colors[color2] }, new[] { colors[color1], colors[color3] }),
            makeQuestion(Question.VexillologyColors, _Vexillology, new[] { "third" }, new[] { colors[color3] }, new[] { colors[color2], colors[color1] }));
    }

    private IEnumerable<object> ProcessVisualImpairment(KMBombModule module)
    {
        var comp = GetComponent(module, "VisualImpairment");
        var fldRoundsFinished = GetIntField(comp, "roundsFinished");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldColor = GetIntField(comp, "color");
        var fldPicture = GetArrayField<string>(comp, "picture");

        // Wait for the first picture to be assigned
        while (fldPicture.Get(nullAllowed: true) == null)
            yield return new WaitForSeconds(.1f);

        var stageCount = GetIntField(comp, "stageCount").Get(min: 2, max: 3);
        var colorsPerStage = new int[stageCount];
        var colorNames = new[] { "Blue", "Green", "Red", "White" };

        while (!fldSolved.Get())
        {
            var newStage = fldRoundsFinished.Get();
            if (newStage >= stageCount)
                break;

            var newColor = fldColor.Get(min: 0, max: 3);
            if (newColor != colorsPerStage[newStage])
                Debug.LogFormat("<Souvenir #{0}> Visual Impairment: stage {1} color changed to {2} ({3}).", _moduleId, newStage, newColor, newColor >= 0 && newColor < 4 ? colorNames[newColor] : "<out of range>");
            colorsPerStage[newStage] = newColor;
            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_VisualImpairment);

        addQuestions(module, colorsPerStage.Select((col, ix) => makeQuestion(Question.VisualImpairmentColors, _VisualImpairment, new[] { ordinal(ix + 1) }, new[] { colorNames[col] })));
    }

    private IEnumerable<object> ProcessWavetapping(KMBombModule module)
    {
        var comp = GetComponent(module, "scr_wavetapping");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var stageColors = GetArrayField<int>(comp, "stageColors").Get(expectedLength: 3);
        var intPatterns = GetArrayField<int>(comp, "intPatterns").Get(expectedLength: 3);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Wavetapping);

        var patternSprites = new Dictionary<int, Sprite[]>();
        var spriteTake = new[] { 4, 4, 3, 2, 2, 2, 2, 2, 9, 4, 40, 13, 4, 8, 21, 38 };
        var spriteSkip = 0;
        for (int i = 0; i < spriteTake.Length; i++)
        {
            patternSprites.Add(i, WavetappingSprites.Skip(spriteSkip).Take(spriteTake[i]).ToArray());
            spriteSkip += spriteTake[i];
        }

        var colorNames = new[] { "Red", "Orange", "Orange-Yellow", "Chartreuse", "Lime", "Green", "Seafoam Green", "Cyan-Green", "Turquoise", "Dark Blue", "Indigo", "Purple", "Purple-Magenta", "Magenta", "Pink", "Gray" };

        var qs = new List<QandA>();

        for (int stage = 0; stage < intPatterns.Length; stage++)
            qs.Add(makeQuestion(Question.WavetappingPatterns, _Wavetapping,
                formatArgs: new[] { ordinal(stage + 1) },
                correctAnswers: new[] { patternSprites[stageColors[stage]][intPatterns[stage]] },
                preferredWrongAnswers: stageColors.SelectMany(stages => patternSprites[stages]).ToArray()));
        for (int stage = 0; stage < 2; stage++)
            qs.Add(makeQuestion(Question.WavetappingColors, _Wavetapping,
                formatArgs: new[] { ordinal(stage + 1) },
                correctAnswers: new[] { colorNames[stageColors[stage]] }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessWhosOnFirst(KMBombModule module)
    {
        var comp = GetComponent(module, "WhosOnFirstComponent");
        var fldSolved = GetField<bool>(comp, "IsSolved", true);
        var propStage = GetProperty<int>(comp, "CurrentStage", true);
        var propButtonsEmerged = GetProperty<bool>(comp, "ButtonsEmerged", true);
        var displayTextMesh = GetField<MonoBehaviour>(comp, "DisplayText", true).Get(); // TextMeshPro
        var propText = GetProperty<string>(displayTextMesh, "text", true);

        while (!propButtonsEmerged.Get())
            yield return new WaitForSeconds(0.1f);

        var displayWords = new string[2];
        for (var i = 0; i < 2; i++)
            while (propStage.Get() == i)
            {
                while (!propButtonsEmerged.Get())
                    yield return new WaitForSeconds(0.1f);

                displayWords[i] = propText.Get();

                while (propButtonsEmerged.Get())
                    yield return new WaitForSeconds(0.1f);
            }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_WhosOnFirst);
        addQuestions(module, displayWords.Select((word, stage) => makeQuestion(Question.WhosOnFirstDisplay, _WhosOnFirst, new[] { ordinal(stage + 1) }, new[] { word }, displayWords)));
    }

    private IEnumerable<object> ProcessWire(KMBombModule module)
    {
        var comp = GetComponent(module, "wireScript");
        var fldSolved = GetField<bool>(comp, "moduleDone");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Wire);

        var dials = GetArrayField<Renderer>(comp, "renderers", isPublic: true).Get(expectedLength: 3);
        addQuestions(module,
            makeQuestion(Question.WireDialColors, _Wire, new[] { "top" }, new[] { dials[0].material.mainTexture.name.Replace("Mat", "") }),
            makeQuestion(Question.WireDialColors, _Wire, new[] { "bottom-left" }, new[] { dials[1].material.mainTexture.name.Replace("Mat", "") }),
            makeQuestion(Question.WireDialColors, _Wire, new[] { "bottom-right" }, new[] { dials[2].material.mainTexture.name.Replace("Mat", "") }),
            makeQuestion(Question.WireDisplayedNumber, _Wire, correctAnswers: new[] { GetIntField(comp, "displayedNumber").Get().ToString() }));
    }

    private IEnumerable<object> ProcessWireOrdering(KMBombModule module)
    {
        var comp = GetComponent(module, "WireOrderingScript");
        var fldSolved = GetField<bool>(comp, "_modSolved");
        var fldChosenColorsDisplay = GetArrayField<int>(comp, "_chosenColorsDis");
        var fldChosenColorsWire = GetArrayField<int>(comp, "_chosenColorsWire");
        var fldChosenDisplayNumbers = GetArrayField<int>(comp, "_chosenDisNum");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_WireOrdering);

        var colors = _attributes[Question.WireOrderingDisplayColor].AllAnswers;
        var chosenColorsDisplay = fldChosenColorsDisplay.Get(expectedLength: 4);
        var chosenDisplayNumbers = fldChosenDisplayNumbers.Get(expectedLength: 4);
        var chosenColorsWire = fldChosenColorsWire.Get(expectedLength: 4);

        var qs = new List<QandA>();
        for (var ix = 0; ix < 4; ix++)
        {
            qs.Add(makeQuestion(Question.WireOrderingDisplayColor, _WireOrdering, new[] { ordinal(ix + 1) }, new[] { colors[chosenColorsDisplay[ix]] }));
            qs.Add(makeQuestion(Question.WireOrderingDisplayNumber, _WireOrdering, new[] { ordinal(ix + 1) }, new[] { chosenDisplayNumbers[ix].ToString() }));
            qs.Add(makeQuestion(Question.WireOrderingWireColor, _WireOrdering, new[] { ordinal(ix + 1) }, new[] { colors[chosenColorsWire[ix]] }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessWireSequence(KMBombModule module)
    {
        var comp = GetComponent(module, "WireSequenceComponent");
        var fldSolved = GetField<bool>(comp, "IsSolved", true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_WireSequence);

        var wireSequence = GetField<IEnumerable>(comp, "wireSequence").Get();

        var counts = new int[3];
        var typeWireConfiguration = wireSequence.GetType().GetGenericArguments()[0];
        var fldNoWire = GetField<bool>(typeWireConfiguration, "NoWire", true);
        var fldColor = GetField<object>(typeWireConfiguration, "Color", true);

        foreach (var item in wireSequence.Cast<object>().Take(12))
            if (!fldNoWire.GetFrom(item))
                counts[(int) fldColor.GetFrom(item)]++;

        var qs = new List<QandA>();
        for (var color = 0; color < 3; color++)
        {
            var preferredWrongAnswers = new string[4];
            for (int i = 0; i < 3; i++)
                preferredWrongAnswers[i] = counts[i].ToString();
            preferredWrongAnswers[3] = (counts[color] == 0 ? 1 : counts[color] - 1).ToString();
            qs.Add(makeQuestion(Question.WireSequenceColorCount, _WireSequence, new[] { new[] { "black", "blue", "red" }[color] }, new[] { counts[color].ToString() }, preferredWrongAnswers));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessYahtzee(KMBombModule module)
    {
        var comp = GetComponent(module, "YahtzeeModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        // This array only changes its contents, it’s never reassigned, so we only need to get it once
        var diceValues = GetArrayField<int>(comp, "_diceValues").Get();

        while (diceValues.Any(v => v == 0))
            yield return new WaitForSeconds(.1f);

        string result;

        // Capture the first roll
        if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 5))
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Yahtzee because the first roll was a Yahtzee.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            result = null;  // don’t yield break here because we need to know when the module is solved in case there are multiple Yahtzees on the bomb
        }
        else if (diceValues.Contains(2) && diceValues.Contains(3) && diceValues.Contains(4) && diceValues.Contains(5) && (diceValues.Contains(1) || diceValues.Contains(6)))
            result = "large straight";
        else if (diceValues.Contains(3) && diceValues.Contains(4) && (
            (diceValues.Contains(1) && diceValues.Contains(2)) ||
            (diceValues.Contains(2) && diceValues.Contains(5)) ||
            (diceValues.Contains(5) && diceValues.Contains(6))))
            result = "small straight";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 4))
            result = "four of a kind";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 3) && Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 2))
            result = "full house";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 3))
            result = "three of a kind";
        else if (Enumerable.Range(1, 6).Count(i => diceValues.Count(val => val == i) == 2) == 2)
            result = "two pairs";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 2))
            result = "pair";
        else
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Yahtzee because the first roll was nothing.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            result = null;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Yahtzee);

        if (result != null)
            addQuestion(module, Question.YahtzeeInitialRoll, correctAnswers: new[] { result });
    }

    private IEnumerable<object> ProcessYellowArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "YellowArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_YellowArrows);

        int letterIndex = GetIntField(comp, "letindex").Get(min: 0, max: 25);
        addQuestion(module, Question.YellowArrowsStartingRow, correctAnswers: new[] { ((char) ('A' + letterIndex)).ToString() });
    }

    private IEnumerable<object> ProcessZoni(KMBombModule module)
    {
        var comp = GetComponent(module, "ZoniModuleScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldIndex = GetIntField(comp, "wordIndex");
        var fldStage = GetIntField(comp, "solvedStages");

        var buttons = GetArrayField<KMSelectable>(comp, "buttons", isPublic: true).Get();
        var words = GetArrayField<string>(comp, "wordlist", isPublic: true).Get();
        int index = fldIndex.Get(0, words.Length - 1);
        int stage = fldStage.Get();
        if (stage != 0)
            throw new AbandonModuleException("‘solvedStages’ did not start at 0: was {0}.", stage);

        var wordsAnswered = new List<int>();
        for (int i = 0; i < buttons.Length; i++)
        {
            var prevInteract = buttons[i].OnInteract;
            buttons[i].OnInteract = delegate
            {
                var ret = prevInteract();
                var st = fldStage.Get();
                if (stage != st)  // If they are equal, the user got a strike
                {
                    wordsAnswered.Add(index);
                    stage = st;
                }
                index = fldIndex.Get();
                return ret;
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Zoni);

        if (wordsAnswered.Count != 3)
            throw new AbandonModuleException("The received number of valid words was not 3: was {0}.", wordsAnswered.Count);

        addQuestions(module,
            makeQuestion(Question.ZoniWords, _Zoni, new[] { "first" }, new[] { words[wordsAnswered[0]] }, words),
            makeQuestion(Question.ZoniWords, _Zoni, new[] { "second" }, new[] { words[wordsAnswered[1]] }, words),
            makeQuestion(Question.ZoniWords, _Zoni, new[] { "third" }, new[] { words[wordsAnswered[2]] }, words));
    }
    #endregion

    #region Twitch Plays
    [NonSerialized]
    public bool TwitchPlaysActive = false;
    [NonSerialized]
    public List<KMBombModule> TwitchAbandonModule = new List<KMBombModule>();
    public static readonly string TwitchHelpMessage = @"!{0} answer 3 [order is from top to bottom, then left to right]";

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (Application.isEditor && !TwitchPlaysActive && command == "tp")
        {
            ActivateTwitchPlaysNumbers();
            TwitchPlaysActive = true;
            yield break;
        }

        if (Application.isEditor)
        {
            var questions = Ut.GetEnumValues<Question>();
            var i = 0;
            do
            {
                Answers[1].OnInteract();
                i++;
            }
            while ((_currentQuestion == null || !_currentQuestion.QuestionText.ContainsIgnoreCase(command)) && i < questions.Length);
            yield break;
        }

        var m = Regex.Match(command.ToLowerInvariant(), @"\A\s*answer\s+(\d)\s*\z");
        if (!m.Success || _isSolved)
            yield break;

        int number;
        if (_animating || _currentQuestion == null)
        {
            yield return "sendtochaterror {0}, there is no question active right now on module {1} (Souvenir).";
            yield break;
        }
        if (!int.TryParse(m.Groups[1].Value, out number) || number <= 0 || number > Answers.Length || Answers[number - 1] == null || !Answers[number - 1].gameObject.activeSelf)
        {
            yield return string.Format("sendtochaterror {{0}}, that’s not a valid answer; give me a number from 1 to {0}.", Answers.Count(a => a != null && a.gameObject.activeSelf));
            yield break;
        }

        yield return null;
        if (_currentQuestion.CorrectIndex == number - 1)
            yield return "awardpoints 1";
        yield return new[] { Answers[number - 1] };
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (true)
        {
            while (_currentQuestion == null)
            {
                if (_isSolved)
                    yield break;
                yield return true;
            }

            Answers[_currentQuestion.CorrectIndex].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    private void ActivateTwitchPlaysNumbers()
    {
        AnswersParent.transform.localPosition = new Vector3(.005f, 0, 0);
        foreach (var gobj in TpNumbers)
            gobj.SetActive(true);
    }
    #endregion
}
