# UX Manager

The UX Manager is a comprehensive framework for building user experience systems in Unity using UI Toolkit. It provides screen management, localization, and event-driven architecture for robust application development.

---

## Overview

The UX Manager provides:

- **Screen Management**: Centralized system for managing screen transitions and lifecycle
- **Localization Service**: Multi-language support with automatic fallback
- **Event-Driven Architecture**: Decoupled communication between screens and systems
- **Service Framework Integration**: Reality Collective Service Framework for dependency injection
- **UI Toolkit-First**: 100% UI Toolkit implementation (no uGUI/Canvas)
- **MVVM Pattern**: Strict separation of data (Models), logic (Handlers), and visuals (Screens)

**Key Concepts:**

- **Screen**: Visual representation of application state (extends `BaseScreen`)
- **Handler**: Business logic controller for screen interactions (extends `BaseScreenHandler`)
- **Profile**: Configuration asset for services and screens
- **Service**: Dependency-injected service providing application functionality (extends `BaseServiceWithConstructor`)
- **Locale**: Language/region code (e.g., "en-US", "es-ES", "fr-FR")

---

## Quick Start

### 1. Create Service Profiles

1. Right-click in Project → Create → Reality Collective → UX Manager
2. Create the following profiles:
   - **ServiceProvidersProfile**: Main service registry
   - **UXScreenManagerProfile**: Screen management configuration
   - **LocalizationServiceProfile**: Localization settings (see Localization Service documentation)

3. Assign to ServiceProvidersProfile:
   - Drag UXScreenManagerProfile into ServiceProvidersProfile inspector
   - Drag LocalizationServiceProfile into ServiceProvidersProfile inspector

### 2. Register Service Providers

1. Create a new GameObject in your startup scene
2. Add component: `ServiceProvidersProfileLoader`
3. Assign your ServiceProvidersProfile
4. This initializes all services on scene load

### 3. Create Your First Screen

Create `MyScreen.cs`:

```csharp
using RealityCollective.UXManager.Services.ScreenManagement;
using UnityEngine.UIElements;

public class MyScreen : BaseScreen
{
    private Label titleLabel;
    private Button actionButton;

    protected override void GenerateUI(VisualElement root)
    {
        ScreenName = ScreenNames.MyScreen;
        
        var panel = UIToolkitExtensions.CreateVisualElement(root, "fullscreen-panel", "myScreenPanel");
        
        titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(panel, "myScreenTitle");
        titleLabel.text = "My Screen";
        
        actionButton = UIToolkitExtensions.CreateVisualElement<Button>(panel, "myScreenActionButton");
        actionButton.text = "Action";
        actionButton.clicked += OnActionButtonClicked;
    }
    
    private void OnActionButtonClicked()
    {
        // Publish to handler via UnityEvent (see Handler below)
    }
}
```

### 4. Create Screen Handler

Create `MyScreenHandler.cs`:

```csharp
using RealityCollective.UXManager.Services.ScreenManagement;
using UnityEngine;

[RequireComponent(typeof(MyScreen))]
public class MyScreenHandler : BaseScreenHandler
{
    public override string ScreenName => ScreenNames.MyScreen;
    
    private MyScreen myScreen;
    
    public override void InitializeHandler()
    {
        myScreen = GetComponent<MyScreen>();
        
        // Subscribe to screen events
        // Perform business logic here
    }
}
```

### 5. Register Screen

1. Open UXScreenManagerProfile
2. Add to screens array:
   - Scene: Select your scene
   - Screen Prefab: Drag your screen GameObject
3. ScreenNames.g.cs regenerates automatically

### 6. Add to Scene

Create a scene with your screen and handler as a Prefab, then register it in UXScreenManagerProfile.

---

## Architecture

### MVVM Pattern

The UX Manager enforces strict MVVM separation:

```
┌─────────────────────────────────────────────┐
│ Screen (View)                               │
│ - UI Elements                               │
│ - UnityEvent callbacks                      │
│ - Display updates only                      │
├─────────────────────────────────────────────┤
│ Handler (ViewModel/Controller)              │
│ - Business logic                            │
│ - Service calls                             │
│ - State management                          │
│ - Screen updates                            │
├─────────────────────────────────────────────┤
│ Services (Model)                            │
│ - Data management                           │
│ - Business operations                       │
│ - External integrations                     │
└─────────────────────────────────────────────┘
```

**Rules:**
- **Screens**: Never contain logic; only display data
- **Handlers**: Never create UI; only coordinate operations
- **Services**: Standalone; no UI dependencies

### Service Framework Pattern

All services follow the Reality Collective Service Framework pattern:

```csharp
// Interface
public interface IMyService : IService { }

// Implementation
[System.Runtime.InteropServices.Guid("unique-guid-here")]
public class MyService : BaseServiceWithConstructor, IMyService
{
    public MyService(string name, uint priority, MyServiceProfile profile)
        : base(name, priority) { }
    
    public override void Initialize() { }
    public override void Start() { }
    public override void Update() { }
    // Other lifecycle methods...
}

// Profile
public class MyServiceProfile : BaseServiceProfile<IMyService> { }
```

**Service Access Pattern:**

```csharp
private IMyService _myService;
protected IMyService MyService 
    => _myService ??= ServiceManager.Instance?.GetService<IMyService>();
```

---

## Screen Management

### Screen Lifecycle

Screens transition through states managed by UXScreenManager:

1. **Initialize**: Screen created, UI generated
2. **Show**: Screen becomes visible and active
3. **Hide**: Screen becomes invisible (may remain in memory)
4. **Destroy**: Screen cleaned up and removed

### Creating Screens

#### 1. Screen Class

```csharp
public class ExampleScreen : BaseScreen
{
    public UnityEvent OnConfirmPressed = new();  // For handler
    
    private Button confirmButton;
    private Label messageLabel;
    
    protected override void GenerateUI(VisualElement root)
    {
        ScreenName = ScreenNames.Example;
        
        var panel = UIToolkitExtensions.CreateVisualElement(root, "fullscreen-panel", "examplePanel");
        
        messageLabel = UIToolkitExtensions.CreateVisualElement<Label>(panel, "exampleMessage");
        messageLabel.text = "Hello!";
        
        confirmButton = UIToolkitExtensions.CreateVisualElement<Button>(panel, "exampleConfirmButton");
        confirmButton.text = "Confirm";
        confirmButton.clicked += () => OnConfirmPressed.Invoke();
    }
    
    public void ShowMessage(string text)
    {
        messageLabel.text = text;
    }
    
    public override void HandleLocaleChanged(string newLocale)
    {
        // Re-localize all text when language changes
        messageLabel.text = LocalizationService?.GetString(LocalizationKeys.ExampleMessage) ?? "Message";
    }
}
```

#### 2. Handler Class

```csharp
[RequireComponent(typeof(ExampleScreen))]
public class ExampleScreenHandler : BaseScreenHandler
{
    public override string ScreenName => ScreenNames.Example;
    
    private ExampleScreen exampleScreen;
    
    private IMyService _myService;
    protected IMyService MyService 
        => _myService ??= ServiceManager.Instance?.GetService<IMyService>();
    
    public override void InitializeHandler()
    {
        exampleScreen = GetComponent<ExampleScreen>();
        exampleScreen.OnConfirmPressed.AddListener(OnConfirmPressed);
    }
    
    private void OnConfirmPressed()
    {
        // Handle user action
        if (MyService != null)
        {
            MyService.PerformAction();
        }
        
        // Navigate to another screen
        UXScreenManager.ShowScreen(ScreenNames.NextScreen);
    }
    
    private void OnDisable()
    {
        if (exampleScreen != null)
        {
            exampleScreen.OnConfirmPressed.RemoveAllListeners();
        }
    }
}
```

### Screen Transitions

**Show a Screen:**

```csharp
UXScreenManager.ShowScreen(ScreenNames.MyScreen);
```

**Hide Current Screen:**

```csharp
UXScreenManager.HideCurrentScreen();
```

**Navigate Sequence:**

```csharp
// Show screen and wait for completion
await UniTask.WaitUntil(() => UXScreenManager.CurrentScreenName == ScreenNames.NextScreen);
```

---

## UI Toolkit Integration

### Creating Custom Components

Custom components extend `VisualElement` and use `UIToolkitExtensions` for element creation:

```csharp
public class MyCustomButton : VisualElement
{
    public const string RootClass = "myCustomButton";
    public const string LabelClass = "myCustomButton__label";
    public const string PressedClass = "myCustomButton--pressed";
    
    private Label label;
    
    public MyCustomButton()
    {
        AddToClassList(RootClass);
        
        label = UIToolkitExtensions.CreateVisualElement<Label>(this, LabelClass);
        
        RegisterCallback<ClickEvent>(_ => OnClicked());
    }
    
    private void OnClicked()
    {
        ToggleInlineStyles(PressedClass);
    }
    
    public void SetText(string text) => label.text = text;
}
```

**CRITICAL RULES:**

- Always use `UIToolkitExtensions.CreateVisualElement<T>(parent, classes...)` - never `new VisualElement()`
- Define CSS class name constants at top of component
- Use BEM naming: `.component__element--modifier`
- All text must be localizable via properties

### Styling with USS

All styling uses USS (Unity Style Sheets). USS is NOT CSS - validate properties carefully.

**Valid USS Properties:**
- `flex-grow`, `flex-shrink`, `flex-wrap`
- `-unity-font-style`, `-unity-font-size`, `-unity-text-align`
- `background-color`, `border-width`, `border-radius`
- `transition`, `translate`, `rotate`

**Invalid USS Properties:**
- ❌ `gap` - Use flex-direction + padding instead
- ❌ `display: grid` - Use flex instead
- ❌ CSS-specific properties

**Example USS:**

```css
.myCustomButton {
    padding: 12px;
    background-color: rgb(0, 120, 200);
    border-radius: 6px;
    flex-direction: row;
}

.myCustomButton__label {
    color: white;
    -unity-font-size: 14px;
    -unity-text-align: middle-center;
}

.myCustomButton--pressed {
    opacity: 0.7;
}
```

---

## Best Practices

### Screen Organization

```
Assets/
  UX/
    Screens/
      MainScreen.cs
      MainScreenHandler.cs
    Handlers/
    CustomComponents/
    Settings/
      Styles/
        ApplicationStyles.uss
        Controls/
          StandardButton.uss
```

### Naming Conventions

- **Screen classes**: `FeatureScreen` (e.g., `ShopScreen`, `SettingsScreen`)
- **Handler classes**: `FeatureScreenHandler` (e.g., `ShopScreenHandler`)
- **CSS classes**: `featureElement`, `feature__subelement`, `feature--state`
- **Events**: `On{Action}` (e.g., `OnPurchasePressed`, `OnSettingChanged`)
- **Services**: `I{Service}Service` (e.g., `IIAPService`, `IAnalyticsService`)

### Event Handling

Always clean up listeners:

```csharp
private void OnEnable()
{
    if (screen != null)
        screen.OnActionPressed.AddListener(HandleAction);
}

private void OnDisable()
{
    if (screen != null)
        screen.OnActionPressed.RemoveListener(HandleAction);  // Remove specific
        // OR
        screen.OnActionPressed.RemoveAllListeners();  // Remove all
}
```

### Localization Integration

All user-facing text must be localizable:

```csharp
public override void GenerateUI(VisualElement root)
{
    // Get reference to localization service
    var localService = ServiceManager.Instance?.GetService<ILocalizationService>();
    
    // Use localized text
    label.text = localService?.GetString(LocalizationKeys.MyScreenTitle) ?? "Title";
}

public override void HandleLocaleChanged(string newLocale)
{
    // Refresh all text when locale changes
    var localService = ServiceManager.Instance?.GetService<ILocalizationService>();
    label.text = localService?.GetString(LocalizationKeys.MyScreenTitle) ?? "Title";
}
```

### Performance Optimization

**Cache service references:**

```csharp
// ✅ GOOD: Single lookup
private IMyService _myService;
protected IMyService MyService 
    => _myService ??= ServiceManager.Instance?.GetService<IMyService>();

// ❌ BAD: Multiple lookups
void Update()
{
    ServiceManager.Instance?.GetService<IMyService>().DoSomething();  // Lookup every frame
}
```

**Query elements once:**

```csharp
// ✅ GOOD: Cache queries
private Button myButton;

protected override void GenerateUI(VisualElement root)
{
    myButton = root.Q<Button>("myButton");  // Query once
}

// ❌ BAD: Query every access
void Update()
{
    root.Q<Button>("myButton").clicked += OnClicked;  // Query every frame
}
```

---

## Troubleshooting

### Screen Not Showing

**Issue**: Screen registered but not visible

**Solutions**:
1. Check ScreenName matches ScreenNames constant
2. Verify screen Prefab has both Screen and Handler components
3. Ensure scene is added to UXScreenManagerProfile
4. Check LocalizationService initialized before screens load
5. Verify UI root has `flex-grow: 1` in CSS

### Localization Keys Not Found

**Issue**: Missing localization key produces console warnings

**Solutions**:
1. Add key to LocalizationServiceProfile
2. Regenerate LocalizationKeys.g.cs (automatic on profile save)
3. Verify key exists in all locale catalogs (en-US.json, es-ES.json, etc.)
4. Check catalog JSON syntax (valid JSON required)

### Service Not Available

**Issue**: ServiceManager returns null for service

**Solutions**:
1. Verify service profile added to ServiceProvidersProfile
2. Check service GUID is unique (use `System.Guid.NewGuid()`)
3. Ensure ServiceProvidersProfileLoader exists in scene
4. Verify assembly definition includes required dependencies

### USS Styles Not Applied

**Issue**: Styles defined but not visible on elements

**Solutions**:
1. Check CSS selector depth (max 3 levels)
2. Verify element has matching CSS class
3. USS property might be invalid - check against valid properties list
4. Use `flex-basis: 0` if flex-grow not working
5. Rebuild project (Assets → Reimport All)

---

## API Reference

### BaseScreen

Base class for all screens.

**Properties:**

- `string ScreenName { get; set; }` - Unique screen identifier
- `VisualElement RootElement { get; }` - Root UIElement for screen

**Methods:**

- `protected abstract void GenerateUI(VisualElement root)` - Create UI elements (called once on initialize)
- `public virtual void HandleLocaleChanged(string newLocale)` - Called when user changes language
- `public virtual void OnScreenShowing()` - Called when screen becomes visible
- `public virtual void OnScreenHiding()` - Called when screen becomes hidden

### BaseScreenHandler

Base class for all screen handlers.

**Properties:**

- `abstract string ScreenName { get; }` - Must match Screen ScreenName
- `protected UXScreenManager UXScreenManager { get; }` - Access screen management

**Methods:**

- `public abstract void InitializeHandler()` - Initialize handler and subscribe to events
- `public virtual void OnScreenShowing()` - Called when screen appears
- `public virtual void OnScreenHiding()` - Called when screen disappears

### UXScreenManager

Centralized screen management system (singleton, accessed via ServiceManager).

**Methods:**

- `void ShowScreen(string screenName)` - Display screen by name
- `void HideCurrentScreen()` - Hide active screen
- `string CurrentScreenName { get; }` - Get active screen name
- `bool IsScreenActive(string screenName)` - Check if screen is visible

---

## See Also

- [Localization Service](com.realitycollective.localizationmanager.md) - Multi-language support
- [Reality Collective Service Framework](https://realitytoolkit.realitycollective.net/) - Dependency injection
- [Unity UI Toolkit Documentation](https://docs.unity3d.com/Manual/UIElements.html) - UI creation reference
