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
        if (lower == "help")
        {
            PrintHelp();
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
            HandleConnect(command);
        }
        else if (lower == "connect")
        {
            _view.PrintLine("Usage: connect <unit_id>");
        }
        else if (lower.StartsWith("patch"))
        {
            HandlePatch(command);
        }
        else
        {
            _view.PrintLine("Unknown command. Type 'help'.");
            _sfx?.PlayError();
        }
    }

    private void PrintHelp()
    {
        _view.PrintBlock("Available commands:\nhelp\nstatus\nwhoami\ninbox\nconnect <unit_id>\npatch\npatch A|B|C");
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
                _sfx?.PlayBeep();
                return;
            }
        }

        _view.PrintLine("Usage: patch or patch A|B|C");
    }
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
