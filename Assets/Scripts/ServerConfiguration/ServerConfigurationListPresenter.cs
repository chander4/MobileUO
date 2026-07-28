using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ServerConfigurationListPresenter : MonoBehaviour
{
    [SerializeField] private ServerConfigurationListItemView serverConfigurationViewInstance;
    [SerializeField] private Button addNewConfigurationButton;
    [SerializeField] private Button supportedServersButton;
    [SerializeField] private Button backButton;
    [SerializeField] private InputField searchInputField;

    public Action AddNewConfigurationButtonClicked;
    public Action<ServerConfiguration> EditButtonClicked;

    private List<ServerConfigurationListItemView> viewsCreated = new List<ServerConfigurationListItemView>();

    private bool showingSupportedServerConfigurations;
    
    private void OnEnable()
    {
        serverConfigurationViewInstance.gameObject.SetActive(false);
        searchInputField.text = "";
        RecreateViews();
        addNewConfigurationButton.onClick.AddListener(OnAddNewConfigurationButtonClicked);
        supportedServersButton.onClick.AddListener(OnSupportedServersClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
        searchInputField.onValueChanged.AddListener(OnSearchChanged);
    }

    private void OnSearchChanged(string value)
    {
        RecreateViews();
    }

    private void OnBackButtonClicked()
    {
        showingSupportedServerConfigurations = false;
        RecreateViews();
    }

    private void OnSupportedServersClicked()
    {
        showingSupportedServerConfigurations = true;
        RecreateViews();
    }

    private void OnDisable()
    {
        addNewConfigurationButton.onClick.RemoveAllListeners();
        searchInputField.onValueChanged.RemoveListener(OnSearchChanged);
        DestroyViews();
    }

    private void RecreateViews()
    {
        DestroyViews();
        if (showingSupportedServerConfigurations)
        {
            addNewConfigurationButton.gameObject.SetActive(false);
            supportedServersButton.gameObject.SetActive(false);
            backButton.gameObject.SetActive(true);
            CreateServerConfigurationItemViews(ServerConfigurationModel.SupportedServerConfigurations, true);
            backButton.transform.SetAsLastSibling();
        }
        else
        {
            addNewConfigurationButton.gameObject.SetActive(true);
            supportedServersButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(false);
            CreateServerConfigurationItemViews(ServerConfigurationModel.ServerConfigurations, false);
            addNewConfigurationButton.transform.SetAsLastSibling();
            supportedServersButton.transform.SetAsLastSibling();
        }
    }

    private void CreateServerConfigurationItemViews(List<ServerConfiguration> configs, bool addInsteadOfEdit)
    {
        // Favorites-first sort only makes sense for the user's own saved configs,
        // not the read-only supported-servers list.
        var filtered = FilterAndSort(configs, sortFavoritesFirst: addInsteadOfEdit == false);

        // Quick connect: when the user has exactly one saved server, highlight it
        // as the obvious action. Based on the total count, not a filtered search result.
        var isQuickConnect = addInsteadOfEdit == false && configs.Count == 1;

        filtered.ForEach(config =>
        {
            var view = Instantiate(serverConfigurationViewInstance.gameObject, serverConfigurationViewInstance.transform.parent).GetComponent<ServerConfigurationListItemView>();
            view.SetServerConfiguration(config);
            view.ShowAddButtonInsteadOfEdit(addInsteadOfEdit);
            view.SetQuickConnect(isQuickConnect);
            view.SelectCallback = ServerConfigurationListItemSelect;
            view.AddOrEditCallback = ServerConfigurationListItemEdit;
            view.gameObject.SetActive(true);
            viewsCreated.Add(view);
        });
    }

    private List<ServerConfiguration> FilterAndSort(List<ServerConfiguration> configs, bool sortFavoritesFirst)
    {
        var searchTerm = searchInputField.text?.Trim() ?? "";
        IEnumerable<ServerConfiguration> query = configs;

        if (string.IsNullOrEmpty(searchTerm) == false)
        {
            query = query.Where(c =>
                (c.Name ?? "").IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (c.UoServerUrl ?? "").IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (c.Description ?? "").IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (sortFavoritesFirst)
        {
            query = query.OrderByDescending(c => c.Favorite).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase);
        }

        return query.ToList();
    }

    private void DestroyViews()
    {
        viewsCreated.ForEach(x => Destroy(x.gameObject));
        viewsCreated.Clear();
    }

    private void ServerConfigurationListItemEdit(ServerConfiguration config)
    {
        if (showingSupportedServerConfigurations)
        {
            AddSupportedConfigAndGoBack(config);
        }
        else
        {
            EditButtonClicked?.Invoke(config);
        }
    }

    private void ServerConfigurationListItemSelect(ServerConfiguration config)
    {
        if (showingSupportedServerConfigurations)
        {
            AddSupportedConfigAndGoBack(config);
        }
        else
        {
            ServerConfigurationModel.MarkConnected(config);
            ServerConfigurationModel.ActiveConfiguration = config;
        }
    }

    private void AddSupportedConfigAndGoBack(ServerConfiguration config)
    {
        if (ServerConfigurationModel.IsServerConfigurationNameValid(config.Name) == false)
        {
            //TODO: Show error saying there already is a config with this name, or something
            return;
        }
        var configClone = config.Clone();
        configClone.SupportedServer = false;
        ServerConfigurationModel.AddServerConfiguration(configClone);
        OnBackButtonClicked();
    }

    private void OnAddNewConfigurationButtonClicked()
    {
        AddNewConfigurationButtonClicked?.Invoke();
        RecreateViews();
    }
}