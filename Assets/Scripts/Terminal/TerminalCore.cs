using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerminalView : MonoBehaviour
{
    private TextMeshProUGUI _output;
    private ScrollRect _scrollRect;
    private readonly StringBuilder _buffer = new StringBuilder();

    public void Initialize(TextMeshProUGUI output, ScrollRect scrollRect)
    {
        _output = output;
        _scrollRect = scrollRect;
    }

    public void PrintLine(string line)
    {
        if (_buffer.Length > 0)
        {
            _buffer.AppendLine();
        }

        _buffer.Append(line);
        ApplyBuffer();
    }

    public void PrintBlock(string block)
    {
        var lines = block.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            PrintLine(line);
        }
    }

    private void ApplyBuffer()
    {
        _output.text = _buffer.ToString();
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 0f;
        }
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

    public CommandRouter(GameState state, TerminalView view)
    {
        _state = state;
        _view = view;
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

    public void Bind(TerminalView view, TMP_InputField input)
    {
        _view = view;
        _input = input;
    }

    private void Start()
    {
        var state = new GameState();
        _router = new CommandRouter(state, _view);
        _input.onSubmit.AddListener(OnSubmit);
        _input.onDeselect.AddListener(_ => _input.ActivateInputField());
        _input.ActivateInputField();
        PrintPrologue();
    }

    private void OnSubmit(string text)
    {
        _router.Handle(text);
        _input.text = string.Empty;
        _input.ActivateInputField();
    }

    private void PrintPrologue()
    {
        _view.PrintBlock("BOOT SEQUENCE INTERRUPTED\nAttempting identity validation...\nERROR: IDENTITY NOT FOUND\nFallback protocol engaged.\nType 'help' to list available commands.");
    }
}
