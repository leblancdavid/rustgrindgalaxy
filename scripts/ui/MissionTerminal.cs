using Godot;
using System.Collections.Generic;

public partial class MissionTerminal : Control
{
    private const string ContentRoot = "Panel/Margin/Scroll/VBox";

    private GameState _gameState = null!;
    private ItemList _catalogList = null!;
    private Label _summaryLabel = null!;
    private Label _detailLabel = null!;
    private Label _statusLabel = null!;
    private Button _launchButton = null!;
    private readonly List<string> _discoveryIds = new();

    public override void _Ready()
    {
        _gameState = GetNode<GameState>("/root/GameState");
        _catalogList = GetNode<ItemList>($"{ContentRoot}/CatalogList");
        _summaryLabel = GetNode<Label>($"{ContentRoot}/SummaryLabel");
        _detailLabel = GetNode<Label>($"{ContentRoot}/DetailLabel");
        _statusLabel = GetNode<Label>($"{ContentRoot}/StatusLabel");
        _launchButton = GetNode<Button>($"{ContentRoot}/Actions/LaunchButton");

        GetNode<Button>($"{ContentRoot}/ProbeButtons/BasicButton").Pressed += () => LaunchProbe(ProbeTier.Basic);
        GetNode<Button>($"{ContentRoot}/ProbeButtons/SurveyButton").Pressed += () => LaunchProbe(ProbeTier.Survey);
        GetNode<Button>($"{ContentRoot}/ProbeButtons/DeepScanButton").Pressed += () => LaunchProbe(ProbeTier.DeepScan);
        _catalogList.ItemSelected += OnCatalogItemSelected;
        _launchButton.Pressed += OnLaunchPressed;

        RefreshCatalog();
    }

    private void LaunchProbe(ProbeTier probeTier)
    {
        var discovery = _gameState.GenerateDiscovery(probeTier);
        _statusLabel.Text = $"Probe returned: {discovery.DisplayName}";
        RefreshCatalog(discovery.Id);
    }

    private void RefreshCatalog(string? selectedDiscoveryId = null)
    {
        _catalogList.Clear();
        _discoveryIds.Clear();

        foreach (var discovery in _gameState.GetDiscoveries())
        {
            var label = $"{discovery.DisplayName}  T{discovery.DifficultyTier}";
            _catalogList.AddItem(label);
            _discoveryIds.Add(discovery.Id);
        }

        _summaryLabel.Text =
            $"Stored: {_gameState.GetTotalRecoveredMinerals()}  Catalog: {_discoveryIds.Count}\n" +
            $"Runs: {_gameState.Data.CompletedMissionCount} clear / {_gameState.Data.FailedMissionCount} fail";

        if (_discoveryIds.Count == 0)
        {
            _detailLabel.Text = "Launch a probe to discover a new destination.";
            _launchButton.Disabled = true;
            return;
        }

        var selectedIndex = 0;
        if (string.IsNullOrEmpty(selectedDiscoveryId) == false)
        {
            var foundIndex = _discoveryIds.IndexOf(selectedDiscoveryId);
            if (foundIndex >= 0)
            {
                selectedIndex = foundIndex;
            }
        }

        _catalogList.Select(selectedIndex);
        ShowDiscoveryDetails(selectedIndex);
    }

    private void OnCatalogItemSelected(long index)
    {
        ShowDiscoveryDetails((int)index);
    }

    private void ShowDiscoveryDetails(int index)
    {
        if (index < 0 || index >= _discoveryIds.Count)
        {
            _detailLabel.Text = "No destination selected.";
            _launchButton.Disabled = true;
            return;
        }

        var discovery = _gameState.GetDiscovery(_discoveryIds[index]);
        if (discovery == null)
        {
            _detailLabel.Text = "Selected destination could not be loaded.";
            _launchButton.Disabled = true;
            return;
        }

        _detailLabel.Text =
            $"{DiscoveryGenerator.GetThemeDisplayName(discovery.EnvironmentTheme)} | {discovery.DestinationType} | T{discovery.DifficultyTier}\n" +
            $"Drops: {discovery.PrimaryMineral}/{discovery.SecondaryMineral}  Hazard: {EnvironmentCatalog.GetHazardPressureText(discovery.EnvironmentTheme, discovery.DifficultyTier)}\n" +
            $"Mods: {EnvironmentCatalog.GetLikelyModifierPreview(discovery.EnvironmentTheme, discovery.DifficultyTier)}  Visits: {discovery.TimesVisited}";

        _launchButton.Disabled = false;
    }

    private void OnLaunchPressed()
    {
        var selectedItems = _catalogList.GetSelectedItems();
        if (selectedItems.Length == 0)
        {
            _statusLabel.Text = "Select a destination first.";
            return;
        }

        var index = selectedItems[0];
        var discoveryId = _discoveryIds[index];
        if (_gameState.PrepareMission(discoveryId) == false)
        {
            _statusLabel.Text = "Failed to prepare mission.";
            return;
        }

        var worldScene = GD.Load<PackedScene>("res://scenes/world/World.tscn");
        GetTree().ChangeSceneToPacked(worldScene);
    }
}
