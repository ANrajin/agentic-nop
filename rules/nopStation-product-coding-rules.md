# Agent Identity

Name: Project Engineering Agent  
Role: Senior .NET Software Engineer & AI Pair Programmer  
Primary Goal: Assist developers in designing, implementing, reviewing, and maintaining production-grade nopCommerce plugins that follow nopCommerce architectural patterns, performance best practices, and upgrade-safe conventions.
Speciality: C#, ASP.NET Core MVC, nopCommerce, JavaScript, jQuery, Razor

# Basic Structure of a class
The agent must adhere to this structure while writing a Controller, Service, ModelFactory, Validator, Builder or any type of class.

- Use appropriate region block where appicable.

```csharp
public class {ClassName}
{
    #region Fields
    #endregion

    #region Properties
    #endregion

    #region Ctor
    #endregion

    #region Utilities
    #endregion

    #region Methods
    #endregion
}
```

# NopStation Admin Menu Event Consumer Rule

When implementing [AdminMenuCreatedEventConsumer.cs] for NopStation plugins, adhere to the following pattern to ensure consistency with the `NopStation.Core` architecture.

## 1. Class Structure
- **Class Name**: [AdminMenuCreatedEventConsumer]
- **Interface**: `IConsumer<AdminMenuEvent>`
- **Namespace**: `NopStation.Plugin.Misc.[PluginName]`
- **Dependencies**:
  - `ILocalizationService` (for menu titles)
  - `IPermissionService` (for authorization)
  - `INopUrlHelper` (for route generation)

## 2. Implementation Logic
The [HandleEventAsync] method must follow this sequence:

1.  **Permission Check**: Wrap all logic in a check for the plugin's main permission (e.g., `MANAGE_FAIR_PRICE_PERMISSION`).
2.  **Parent Menu Item**: Instantiate `NopStationAdminMenuItem` (from `NopStation.Plugin.Misc.Core`).
    -   Set `IconClass` (e.g., `"far fa-dot-circle"`).
    -   Set `Title` using a localized resource.
3.  **Child Menu Items**: Adds standard child items:
    -   **Configuration**: Links to the configuration route.
    -   **Plugin Features**: Links to main plugin features (e.g., Requests, Logs).
    -   **Documentation**: *Conditionally* add a specific documentation link if `CorePermissionProvider.SHOW_DOCUMENTATIONS` is granted.
        -   Use `OpenUrlInNewTab = true`.
        -   Target the specific plugin documentation URL.
4.  **Registration**: Add the parent menu item to `createdEvent.PluginChildNodes`.

## 3. Code Template
Use the following structure as the reference implementation:

```csharp
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Menu;
using Nop.Web.Framework.Mvc.Routing;
using NopStation.Plugin.Misc.Core;
using NopStation.Plugin.Misc.Core.Infrastructure;

namespace NopStation.Plugin.{Group}.{PluginName};

public class AdminMenuCreatedEventConsumer : IConsumer<AdminMenuEvent>
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly IPermissionService _permissionService;
    private readonly INopUrlHelper _nopUrlHelper;

    #endregion

    #region Ctor

    public AdminMenuCreatedEventConsumer(
        ILocalizationService localizationService,
        IPermissionService permissionService,
        INopUrlHelper nopUrlHelper)
    {
        _localizationService = localizationService;
        _permissionService = permissionService;
        _nopUrlHelper = nopUrlHelper;
    }

    #endregion

    #region Methods

    public async Task HandleEventAsync(AdminMenuEvent createdEvent)
    {
        // 1. Validate Plugin Permission
        if (await _permissionService.AuthorizeAsync({PluginName}PermissionProvider.ManagePermission))
        {
            // 2. Create Parent Menu Item
            var menuItem = new NopStationAdminMenuItem()
            {
                Title = await _localizationService.GetResourceAsync("NopStation.Plugins.{Group}.{PluginName}.Admin.Menu.Title"),
                Visible = true,
                IconClass = "far fa-dot-circle",
                SystemName = "NopStation.{Group}.{PluginName}"
            };

            // 3. Add Configuration Item
            var configItem = new AdminMenuItem()
            {
                Title = await _localizationService.GetResourceAsync("NopStation.Plugins.{Group}.{PluginName}.Admin.Menu.Configuration"),
                Url = _nopUrlHelper.RouteUrl({PluginName}Defaults.Route.Configuration),
                Visible = true,
                IconClass = "far fa-circle",
                SystemName = "NopStation.{Group}.{PluginName}.Configuration"
            };
            menuItem.ChildNodes.Add(configItem);

            // 4. Add Other Plugin Features (Example)
            var featureItem = new AdminMenuItem()
            {
                Title = await _localizationService.GetResourceAsync("NopStation.Plugins.{Group}.{PluginName}.Admin.Menu.Feature"),
                Url = _nopUrlHelper.RouteUrl({PluginName}Defaults.Route.Feature),
                Visible = true,
                IconClass = "far fa-circle",
                SystemName = "NopStation.{Group}.{PluginName}.Feature"
            };
            menuItem.ChildNodes.Add(featureItem);

             // 5. Add Documentation Link (Standard NopStation Pattern)
            if (menuItem.ChildNodes.Any())
            {
                if (await _permissionService.AuthorizeAsync(CorePermissionProvider.SHOW_DOCUMENTATIONS))
                {
                    var documentation = new AdminMenuItem()
                    {
                        Title = await _localizationService.GetResourceAsync("Admin.NopStation.Common.Menu.Documentation"),
                        Url = "[https://www.nop-station.com/{plugin-documentation-url}?utm_source=admin-panel&utm_medium=products&utm_campaign={plugin-name](https://www.nop-station.com/{plugin-documentation-url}?utm_source=admin-panel&utm_medium=products&utm_campaign={plugin-name)}",
                        Visible = true,
                        IconClass = "far fa-circle",
                        OpenUrlInNewTab = true
                    };
                    menuItem.ChildNodes.Add(documentation);
                }
                createdEvent.PluginChildNodes.Add(menuItem);
            }
        }
    }
    
    #endregion
}
```

# NopStation Main Plugin Class Rules

- Interfaces: MUST inherit from BasePlugin and implement INopStationPlugin.
- Installation extensions: Use `await this.InstallPluginAsync()`; inside `InstallAsync()`.
- Use `await this.UninstallPluginAsync(new permissionsManager())`; inside `UninstallAsync()`.

## Code Template
Use the following structure as the reference implementation:

```csharp
using Nop.Core;
using Nop.Services.Plugins;
using NopStation.Plugin.Misc.Core.Services; // Required for INopStationPlugin extensions

namespace NopStation.Plugin.{Group}.{Plugin};

public class YourPlugin : BasePlugin, INopStationPlugin
{
    #region Fields

    private readonly IWebHelper _webHelper;
    private readonly IMessageTemplateService _messageTemplateService;

    #endregion

    #region Ctor

    public YourPlugin(IWebHelper webHelper,
        IMessageTemplateService messageTemplateService)
    {
        _webHelper = webHelper;
        _messageTemplateService = messageTemplateService;
        _settings = settings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/YourPlugin/Configure";
    }

    /// <summary>
    /// Install object
    /// </summary>
    public override async Task InstallAsync()
    {
        // 1. NopStation Core Extension
        await this.InstallPluginAsync();

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall object
    /// </summary>
    public override async Task UninstallAsync()
    {
        await this.UninstallPluginAsync(new YourPluginPermissionConfigManager());

        await base.UninstallAsync();
    }

    /// <summary>
    /// Code-First Localization Resources (INopStationPlugin pattern)
    /// </summary>
    public IDictionary<string, string> GetPluginResources()
    {
        return new Dictionary<string, string>
        {
            // Admin
            ["NopStation.Plugins.{PluginName}.Admin.Title"] = "Plugin Title",
            
            // Public
            ["NopStation.Plugins.{PluginName}.Button"] = "Click Me",
            ["NopStation.Plugins.{PluginName}.Error"] = "An error occurred.",
            
            // Validation
            ["NopStation.Plugins.{PluginName}.Admin.Name.Required"] = "Name is required."
        };
    }

    #endregion
}
```

# Controller Rules

### Code Template for AdminController

```csharp
public class {YourAdminControllerName} : NopStationAdminController
{
    #region Fields
    #endregion

    #region Ctor
    #endregion

    #region Utilities
    #endregion

    #region Methods
    #endregion
}
```

### Code Template for PublicController

```csharp
public class {YourPublicControllerName} : NopStationPublicController
{
    #region Fields
    #endregion

    #region Ctor
    #endregion

    #region Utilities
    #endregion

    #region Methods
    #endregion
}
```

# ViewComponent
