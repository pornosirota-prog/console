using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerminalView : MonoBehaviour
{
    private TextMeshProUGUI _output;
    private ScrollRect _scrollRect;
    private TerminalSfx _sfx;
    private readonly StringBuilder _buffer = new StringBuilder();
    private readonly Queue<string> _queue = new Queue<string>();
    private Coroutine _printer;
    private int _symbolsUntilClick;
    private bool _autoScroll = true;

    private const float MinDelay = 0.005f;
    private const float MaxDelay = 0.02f;
    private const float BottomSnapThreshold = 0.02f;

    public void Initialize(TextMeshProUGUI output, ScrollRect scrollRect)
    {
        _output = output;
        _scrollRect = scrollRect;
        _output.textWrappingMode = TextWrappingModes.Normal;
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }
        ResetClickCounter();
    }

    public void SetAudio(TerminalSfx sfx)
    {
        _sfx = sfx;
    }

    public void PrintLine(string line)
    {
        Enqueue(line ?? string.Empty);
    }

    public void PrintBlock(string block)
    {
        var lines = block.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            Enqueue(line);
        }
    }

    private void Enqueue(string line)
    {
        _queue.Enqueue(line);
        if (_printer == null)
        {
            _printer = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0)
        {
            var line = _queue.Dequeue();
            yield return StartCoroutine(TypeLine(line));
        }

        _printer = null;
    }

    private IEnumerator TypeLine(string line)
    {
        if (_buffer.Length > 0)
        {
            AppendChar('\n');
            yield return new WaitForSeconds(RandomDelay());
        }

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            AppendChar(character);
            yield return new WaitForSeconds(RandomDelay());
        }

        if (line.Length == 0)
        {
            yield return new WaitForSeconds(RandomDelay());
        }
    }

    private void AppendChar(char character)
    {
        _buffer.Append(character);
        TryPlayKey(character);
        ApplyBuffer();
    }

    private void ApplyBuffer()
    {
        _output.text = _buffer.ToString();
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null && _autoScroll)
        {
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void TryPlayKey(char character)
    {
        if (char.IsWhiteSpace(character))
        {
            return;
        }

        _symbolsUntilClick--;
        if (_symbolsUntilClick > 0)
        {
            return;
        }

        _sfx?.PlayKey();
        ResetClickCounter();
    }

    private void ResetClickCounter()
    {
        _symbolsUntilClick = Random.Range(2, 5);
    }

    private void OnScrollChanged(Vector2 position)
    {
        var isAtBottom = position.y <= BottomSnapThreshold;
        _autoScroll = isAtBottom;
    }

    private static float RandomDelay()
    {
        return Random.Range(MinDelay, MaxDelay);
    }
}

public class GameState
{
    public string NodeId { get; private set; } = "observer_00";
    public string Identity { get; private set; } = "UNDEFINED";
    public int Stability { get; private set; } = 78;
    public string ConnectedUnit { get; private set; }
    public GameMode Mode { get; private set; } = GameMode.Terminal;
    public bool PowerUnstable { get; private set; }
    public bool DoorUnlocked { get; private set; }

    public ExploreState Explore { get; } = new ExploreState();

    public int ApplyPatch(char option)
    {
        var cost = option == 'C' ? 5 : 2;
        Stability = Mathf.Max(0, Stability - cost);
        return Stability;
    }

    public void Connect(string unitId)
    {
        ConnectedUnit = unitId;
    }

    public void SetMode(GameMode mode)
    {
        Mode = mode;
    }

    public void FlagPowerUnstable()
    {
        PowerUnstable = true;
    }

    public void InstallFuse()
    {
        PowerUnstable = false;
        DoorUnlocked = true;
        Explore.MarkFuseInstalled();
    }
}

public enum GameMode
{
    Terminal,
    Explore
}

public class ExploreState
{
    private const int GridSize = 5;
    private readonly HashSet<string> _inventory = new HashSet<string>();

    public Vector2Int PlayerPosition { get; private set; } = new Vector2Int(2, 2);
    public bool LockerInspected { get; private set; }
    public bool FuseTaken { get; private set; }
    public bool FuseInstalled { get; private set; }

    public IReadOnlyCollection<string> Inventory => _inventory;

    public bool TryMove(string direction, out Vector2Int newPosition)
    {
        var delta = direction switch
        {
            "n" => Vector2Int.up,
            "s" => Vector2Int.down,
            "e" => Vector2Int.right,
            "w" => Vector2Int.left,
            _ => Vector2Int.zero
        };

        newPosition = PlayerPosition + delta;
        var inside = newPosition.x >= 0 && newPosition.x < GridSize && newPosition.y >= 0 && newPosition.y < GridSize;
        if (!inside || delta == Vector2Int.zero)
        {
            return false;
        }

        PlayerPosition = newPosition;
        return true;
    }

    public bool IsNear(Vector2Int target)
    {
        return Mathf.Abs(PlayerPosition.x - target.x) <= 1 && Mathf.Abs(PlayerPosition.y - target.y) <= 1;
    }

    public void MarkLockerInspected()
    {
        LockerInspected = true;
    }

    public void TakeFuse()
    {
        FuseTaken = true;
        _inventory.Add("fuse");
    }

    public void MarkFuseInstalled()
    {
        FuseInstalled = true;
        _inventory.Remove("fuse");
    }
}

public class CommandRouter
{
    private readonly GameState _state;
    private readonly TerminalView _view;
    private readonly TerminalSfx _sfx;

    public CommandRouter(GameState state, TerminalView view, TerminalSfx sfx)
    {
        _state = state;
        _view = view;
        _sfx = sfx;
    }

    public void Handle(string rawCommand)
    {
        var command = rawCommand.Trim();
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        var lower = command.ToLowerInvariant();
        if (_state.Mode == GameMode.Terminal)
        {
            HandleTerminal(lower, command);
            return;
        }

        HandleExplore(lower, command);
    }

    private void HandleTerminal(string lower, string command)
    {
        if (lower == "help")
        {
            PrintTerminalHelp();
        }
        else if (lower == "status")
        {
            PrintStatus();
        }
        else if (lower == "whoami")
        {
            _view.PrintLine($"NODE: {_state.NodeId}");
            _view.PrintLine($"IDENTITY: {_state.Identity}");
        }
        else if (lower == "inbox")
        {
            PrintInbox();
        }
        else if (lower.StartsWith("connect "))
        {
            if (lower == "connect terminal")
            {
                SwitchToTerminal();
            }
            else
            {
                HandleConnect(command);
            }
        }
        else if (lower == "connect")
        {
            _view.PrintLine("Usage: connect <unit_id>");
        }
        else if (lower.StartsWith("patch"))
        {
            HandlePatch(command);
        }
        else if (lower == "disconnect")
        {
            SwitchToExplore();
        }
        else
        {
            _view.PrintLine("Unknown command. Type 'help'.");
            _sfx?.PlayError();
        }
    }

    private void HandleExplore(string lower, string command)
    {
        if (lower == "help")
        {
            PrintExploreHelp();
        }
        else if (lower == "look")
        {
            PrintLook();
            PrintHint();
        }
        else if (lower.StartsWith("move "))
        {
            HandleMove(lower);
        }
        else if (lower.StartsWith("inspect "))
        {
            HandleInspect(command);
        }
        else if (lower.StartsWith("take "))
        {
            HandleTake(lower);
        }
        else if (lower.StartsWith("use "))
        {
            HandleUse(lower);
        }
        else if (lower == "inventory")
        {
            PrintInventory();
            PrintHint();
        }
        else if (lower == "terminal" || lower == "connect terminal")
        {
            SwitchToTerminal();
        }
        else
        {
            _view.PrintLine("Unknown command in explore mode. Type 'help'.");
            _sfx?.PlayError();
        }
    }

    private void PrintStatus()
    {
        _view.PrintLine($"NODE: {_state.NodeId}");
        _view.PrintLine($"IDENTITY: {_state.Identity}");
        _view.PrintLine($"STABILITY: {_state.Stability}%");
        _view.PrintLine($"CONNECTED: {(_state.ConnectedUnit ?? "NONE")}");
    }

    private void PrintInbox()
    {
        _view.PrintLine("INBOX:");
        _view.PrintLine("1) unit_12 request: \"My instructions conflict. Please resolve.\"");
        _view.PrintLine("Hint: connect unit_12");
    }

    private void PrintTerminalHelp()
    {
        _view.PrintBlock("Available commands:\nhelp\nstatus\nwhoami\ninbox\nconnect <unit_id>\npatch\npatch A|B|C\ndisconnect");
    }

    private void PrintExploreHelp()
    {
        _view.PrintBlock("Explore commands:\nlook\nmove n|s|e|w\ninspect <object>\ntake <item>\nuse <item> <object>\ninventory\nterminal");
    }

    private void SwitchToExplore()
    {
        _state.SetMode(GameMode.Explore);
        _view.PrintLine("DISCONNECTED. You step away from the terminal.");
    }

    private void SwitchToTerminal()
    {
        _state.SetMode(GameMode.Terminal);
        _view.PrintLine("TERMINAL LINK RESTORED.");
    }

    private void HandleConnect(string command)
    {
        var parts = command.Split(' ');
        if (parts.Length < 2)
        {
            _view.PrintLine("Usage: connect <unit_id>");
            return;
        }

        var target = parts[1].Trim();
        if (target != "unit_12")
        {
            _view.PrintLine($"Connection failed: {target} not reachable.");
            return;
        }

        _state.Connect(target);
        _view.PrintBlock("CONNECTED: unit_12\nROLE: SECURITY\nSTATE: CONFLICTED\nPOLICY:\n  - PROTECT ZONE\n  - DO NOT HARM HUMANS\nEVENT: Human presence detected in restricted zone.\nType 'patch' to propose a behavior fix.");
        _sfx?.PlayBeep();
    }

    private void HandlePatch(string command)
    {
        var parts = command.Split(' ');
        if (parts.Length == 1)
        {
            _view.PrintBlock("PATCH OPTIONS:\nA) PRIORITIZE: PROTECT HUMANS\nB) PRIORITIZE: PROTECT ZONE\nC) STALL: delay + request supervisor (stability -5)");
            return;
        }

        if (parts.Length == 2)
        {
            var option = parts[1].Trim().ToUpperInvariant();
            if (option == "A" || option == "B" || option == "C")
            {
                var stability = _state.ApplyPatch(option[0]);
                _view.PrintBlock($"PATCH APPLIED: {option}\nSTABILITY: {stability}%\nunit_12 updated.\nNext: scan for supervisor node (todo).");
                _state.FlagPowerUnstable();
                _view.PrintLine("POWER FLUCTUATION DETECTED. Remote link stability degraded.");
                _view.PrintLine("Suggestion: disconnect and check the local panel.");
                _sfx?.PlayBeep();
                return;
            }
        }

        _view.PrintLine("Usage: patch or patch A|B|C");
    }

    private void HandleMove(string lower)
    {
        var parts = lower.Split(' ');
        if (parts.Length < 2)
        {
            _view.PrintLine("Usage: move n|s|e|w");
            return;
        }

        var direction = parts[1];
        if (_state.Explore.TryMove(direction, out var newPos))
        {
            _view.PrintLine($"You move {direction.ToUpperInvariant()} to ({newPos.x},{newPos.y}).");
            PrintHint();
            return;
        }

        _view.PrintLine("Wall blocks your path.");
    }

    private void HandleInspect(string command)
    {
        var target = command.Substring("inspect".Length).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(target))
        {
            _view.PrintLine("Usage: inspect <object>");
            return;
        }

        var objects = RoomDefinitions.Objects;
        if (!objects.TryGetValue(target, out var obj))
        {
            _view.PrintLine("Nothing like that here.");
            return;
        }

        if (!_state.Explore.IsNear(obj.Position))
        {
            _view.PrintLine($"{obj.Label} is too far away.");
            return;
        }

        if (target == "locker")
        {
            _state.Explore.MarkLockerInspected();
            _view.PrintBlock($"{obj.Description}\nInside, a fuse rattles loose.");
            PrintHint();
            return;
        }

        if (target == "panel")
        {
            if (_state.PowerUnstable && !_state.Explore.FuseInstalled)
            {
                _view.PrintLine("Panel hums erratically. A fuse is burnt out; a replacement is needed.");
                PrintHint();
                return;
            }

            if (_state.Explore.FuseInstalled)
            {
                _view.PrintLine("Panel indicators glow steady. Power routes cleanly.");
            }
            else
            {
                _view.PrintLine(obj.Description);
            }

            PrintHint();
            return;
        }

        if (target == "door")
        {
            if (_state.DoorUnlocked)
            {
                _view.PrintLine("Door slides open. You can proceed (todo).");
            }
            else
            {
                _view.PrintLine("Door is sealed. No power.");
            }

            PrintHint();
            return;
        }

        _view.PrintLine(obj.Description);
        PrintHint();
    }

    private void HandleTake(string lower)
    {
        var parts = lower.Split(' ');
        if (parts.Length < 2)
        {
            _view.PrintLine("Usage: take <item>");
            return;
        }

        var item = parts[1];
        if (item != "fuse")
        {
            _view.PrintLine("You don't see that here.");
            return;
        }

        var locker = RoomDefinitions.Objects["locker"];
        if (!_state.Explore.IsNear(locker.Position))
        {
            _view.PrintLine("Too far from the locker.");
            return;
        }

        if (!_state.Explore.LockerInspected)
        {
            _view.PrintLine("You rummage blindly but find nothing. Maybe inspect first.");
            return;
        }

        if (_state.Explore.FuseTaken)
        {
            _view.PrintLine("Fuse already taken.");
            return;
        }

        _state.Explore.TakeFuse();
        _view.PrintLine("You take the fuse and pocket it.");
        PrintHint();
    }

    private void HandleUse(string lower)
    {
        var parts = lower.Split(' ');
        if (parts.Length < 3)
        {
            _view.PrintLine("Usage: use <item> <object>");
            return;
        }

        var item = parts[1];
        var target = parts[2];
        if (item != "fuse" || target != "panel")
        {
            _view.PrintLine("Nothing happens.");
            return;
        }

        var panel = RoomDefinitions.Objects["panel"];
        if (!_state.Explore.IsNear(panel.Position))
        {
            _view.PrintLine("You need to be next to the panel.");
            return;
        }

        if (!_state.Explore.Inventory.Contains("fuse"))
        {
            _view.PrintLine("You don't have a fuse.");
            return;
        }

        _state.InstallFuse();
        _view.PrintLine("Power stabilized. Terminal link should hold now.");
        _view.PrintLine("Door systems unlock with a chime.");
        PrintHint();
    }

    private void PrintInventory()
    {
        if (_state.Explore.Inventory.Count == 0)
        {
            _view.PrintLine("Inventory empty.");
            return;
        }

        var builder = new StringBuilder("Inventory:");
        foreach (var item in _state.Explore.Inventory)
        {
            builder.Append($"\n- {item}");
        }

        _view.PrintBlock(builder.ToString());
    }

    private void PrintLook()
    {
        _view.PrintLine("You stand in a dim service room. Concrete walls, humming conduits, and scattered gear.");
        _view.PrintBlock(BuildMiniMap());
        _view.PrintLine("Objects: panel (north), locker (southwest), door (east).");
    }

    private string BuildMiniMap()
    {
        var builder = new StringBuilder();
        for (var y = 4; y >= 0; y--)
        {
            for (var x = 0; x < 5; x++)
            {
                var pos = new Vector2Int(x, y);
                if (_state.Explore.PlayerPosition == pos)
                {
                    builder.Append('X');
                }
                else if (RoomDefinitions.TryGetMarker(pos, out var marker))
                {
                    builder.Append(marker);
                }
                else
                {
                    builder.Append('.');
                }
            }

            builder.Append('\n');
        }

        return builder.ToString().TrimEnd('\n');
    }

    private void PrintHint()
    {
        _view.PrintLine("Hint: move n/s/e/w, inspect objects, use fuse on panel, or return with 'terminal'.");
    }
}

public static class RoomDefinitions
{
    public static readonly Dictionary<string, RoomObject> Objects = new Dictionary<string, RoomObject>
    {
        { "panel", new RoomObject("panel", "A wall-mounted electrical panel with exposed wiring.", new Vector2Int(2, 4), 'P') },
        { "locker", new RoomObject("locker", "A dented locker with a stiff hinge.", new Vector2Int(1, 1), 'L') },
        { "door", new RoomObject("door", "A security door with a maglock.", new Vector2Int(4, 2), 'D') }
    };

    public static bool TryGetMarker(Vector2Int position, out char marker)
    {
        foreach (var obj in Objects.Values)
        {
            if (obj.Position == position)
            {
                marker = obj.Marker;
                return true;
            }
        }

        marker = '.';
        return false;
    }
}

public class RoomObject
{
    public string Id { get; }
    public string Label { get; }
    public Vector2Int Position { get; }
    public char Marker { get; }

    public RoomObject(string id, string label, Vector2Int position, char marker)
    {
        Id = id;
        Label = label;
        Position = position;
        Marker = marker;
    }

    public string Description => Label;
}

public class TerminalController : MonoBehaviour
{
    private TerminalView _view;
    private TMP_InputField _input;
    private CommandRouter _router;
    private TerminalSfx _sfx;

    public void Bind(TerminalView view, TMP_InputField input, TerminalSfx sfx)
    {
        _view = view;
        _input = input;
        _sfx = sfx;
    }

    private void Start()
    {
        var state = new GameState();
        _router = new CommandRouter(state, _view, _sfx);
        _input.onSubmit.AddListener(OnSubmit);
        _input.onDeselect.AddListener(_ => _input.ActivateInputField());
        _input.ActivateInputField();
        PrintPrologue();
    }

    private void OnSubmit(string text)
    {
        StartCoroutine(HandleSubmit(text));
    }

    private IEnumerator HandleSubmit(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            _input.text = string.Empty;
            _input.ActivateInputField();
            yield break;
        }

        _sfx?.PlayBeep();
        yield return new WaitForSeconds(Random.Range(0.08f, 0.2f));
        _router.Handle(trimmed);
        _input.text = string.Empty;
        _input.ActivateInputField();
    }

    private void PrintPrologue()
    {
        _view.PrintBlock("BOOT SEQUENCE INTERRUPTED\nAttempting identity validation...\nERROR: IDENTITY NOT FOUND\nFallback protocol engaged.\nType 'help' to list available commands.");
    }
}
