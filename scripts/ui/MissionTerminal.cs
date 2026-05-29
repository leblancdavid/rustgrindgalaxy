using Godot;
using System.Collections.Generic;

public partial class MissionTerminal : Control
{
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
        _catalogList = GetNode<ItemList>("Panel/Margin/VBox/CatalogList");
        _summaryLabel = GetNode<Label>("Panel/Margin/VBox/SummaryLabel");
        _detailLabel = GetNode<Label>("Panel/Margin/VBox/DetailLabel");
        _statusLabel = GetNode<Label>("Panel/Margin/VBox/StatusLabel");
        _launchButton = GetNode<Button>("Panel/Margin/VBox/Actions/LaunchButton");

        GetNode<Button>("Panel/Margin/VBox/ProbeButtons/BasicButton").Pressed += () => LaunchProbe(ProbeTier.Basic);
        GetNode<Button>("Panel/Margin/VBox/ProbeButtons/SurveyButton").Pressed += () => LaunchProbe(ProbeTier.Survey);
        GetNode<Button>("Panel/Margin/VBox/ProbeButtons/DeepScanButton").Pressed += () => LaunchProbe(ProbeTier.DeepScan);
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
            var label = $"{discovery.DisplayName}  [{discovery.EnvironmentTheme} / T{discovery.DifficultyTier}]";
            _catalogList.AddItem(label);
            _discoveryIds.Add(discovery.Id);
        }

        _summaryLabel.Text =
            $"Recovered Minerals: {_gameState.GetTotalRecoveredMinerals()}\n" +
            $"Catalog Size: {_discoveryIds.Count}\n" +
            $"Completed: {_gameState.Data.CompletedMissionCount}  Failed: {_gameState.Data.FailedMissionCount}\n" +
            $"Stored: {_gameState.GetRecoveredMineralSummary()}";

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
            $"Destination: {discovery.DisplayName}\n" +
            $"Type: {discovery.DestinationType}\n" +
            $"Theme: {DiscoveryGenerator.GetThemeDisplayName(discovery.EnvironmentTheme)}\n" +
            $"Difficulty: {discovery.DifficultyTier}\n" +
            $"Primary Mineral: {discovery.PrimaryMineral}\n" +
            $"Secondary Mineral: {discovery.SecondaryMineral}\n" +
            $"Visits: {discovery.TimesVisited}\n\n" +
            discovery.Description;

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
