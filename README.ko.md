# Aprillz.MewUI

NativeAOT + Trim 앱을 목표로 하는, 코드 기반(code-first) .NET GUI 라이브러리입니다.

**상태:** 실험적 프로토타입 버전입니다(기능/동작/API는 변경될 수 있습니다).

**참고:** 이 저장소의 대부분의 코드는 GPT의 도움으로 작성되었습니다.

**샘플 프로젝트 NativeAOT + Trimmed 빌드 출력:** 단일 exe 약 `2.2 MB`

## C# 마크업 예시

```csharp
var window = new Window()
    .Title("Hello MewUI")
    .Size(520, 360)
    .Padding(12)
    .Content(
        new StackPanel()
            .Spacing(8)
            .Children(
                new Label()
                    .Text("Hello, Aprillz.MewUI")
                    .FontSize(18)
                    .Bold(),
                new Button()
                    .Content("Quit")
                    .OnClick(() => Application.Quit())
            )
    );

Application.Run(window);
```

## 컨셉

MewUI는 아래 3가지를 최우선으로 둔 code-first UI 라이브러리입니다:
- **XAML 없이 Fluent한 C# 마크업**으로 UI 트리 구성
- **AOT 친화적 바인딩** (`ObservableValue<T>`, 델리게이트 기반, 리플렉션 지양)
- **NativeAOT + Trim 친화**(interop는 `LibraryImport`)

범위 밖(당분간):
- XAML/WPF 완전 호환
- 방대한 컨트롤 카탈로그 및 디자이너 툴링

## 빠른 시작

필수: .NET SDK (`net10.0-windows`).

빌드:
- `dotnet build .\\MewUI.slnx -c Release`

샘플 실행:
- `dotnet run --project .\\samples\\MewUI.Sample\\MewUI.Sample.csproj -c Release`

배포 (NativeAOT + Trim, 용량 중심):
- `dotnet publish .\\samples\\MewUI.Sample\\MewUI.Sample.csproj -c Release -p:PublishProfile=win-x64-trimmed`

## NativeAOT / Trim

- 기본적으로 trimming-safe를 지향합니다(명시적 코드 경로, 리플렉션 기반 바인딩 없음).
- Windows interop은 NativeAOT 호환을 위해 소스 생성 P/Invoke(`LibraryImport`)를 사용합니다.
- interop/dynamic 기능을 추가했다면, 아래 publish 설정으로 반드시 검증하는 것을 권장합니다.

로컬에서 확인:
- Publish: `dotnet publish .\\samples\\MewUI.Sample\\MewUI.Sample.csproj -c Release -p:PublishProfile=win-x64-trimmed`
- 출력 확인: `samples\\MewUI.Sample\\bin\\Release\\net10.0-windows\\win-x64\\publish\\trimmed\\`

프로젝트는 publish 결과물 용량을 줄이는 방향으로 설계되어 있지만, 최종 용량은 환경/설정에 따라 달라집니다:
- .NET SDK 버전, RID, linker/ILC 옵션
- 사용하는 렌더링 백엔드(Direct2D vs GDI)
- 포함하는 폰트/리소스

참고(샘플, `win-x64-trimmed`):
- `Aprillz.MewUI Demo.exe` 약 `2,257 KB`

## 상태/바인딩(AOT 친화)

바인딩은 리플렉션 없이, 명시적/델리게이트 기반입니다:

```csharp
using Aprillz.MewUI.Binding;
using Aprillz.MewUI.Controls;

var percent = new ObservableValue<double>(0.25);

var slider = new Slider().BindValue(percent);
var label  = new Label().BindText(percent, v => $"Percent ({v:P0})");
```

## 테마(Theme)

테마는 두 부분으로 구성됩니다:
- `Palette` - 색상(배경/Accent 기반 파생 색 포함)
- `Theme` - 색 이외의 파라미터(코너 라디우스, 기본 폰트 등 + `Palette`)

Accent 변경:

```csharp
Theme.Current = Theme.Current.WithAccent(Aprillz.MewUI.Primitives.Color.FromRgb(214, 176, 82));
```

## 컨트롤 / 패널

컨트롤:
- `Label`, `Button`, `TextBox`
- `CheckBox`, `RadioButton`
- `ListBox`, `ComboBox`
- `Slider`, `ProgressBar`
- `Window`

패널:
- `Grid` (row/column: `Auto`, `*`, pixel)
- `StackPanel` (가로/세로 + Spacing)
- `DockPanel` (도킹 + 마지막 자식 채우기)
- `UniformGrid` (균등 셀)
- `WrapPanel` (줄바꿈 + Item size + Spacing)

## 렌더링 백엔드

렌더링은 아래 추상화로 분리됩니다:
- `IGraphicsFactory` / `IGraphicsContext`

샘플은 기본적으로 Direct2D를 사용하며, GDI 백엔드도 제공됩니다.

## 플랫폼 추상화

윈도우/메시지 루프는 플랫폼 계층으로 추상화되어 있으며, 현재는 Windows 구현(`Win32PlatformHost`)을 제공합니다.
추후 Linux/macOS 포팅 시 이 계층에 백엔드를 추가하는 방식으로 확장하는 것을 목표로 합니다.

## DPI

샘플 EXE에는 PerMonitorV2 DPI awareness가 활성화된 `app.manifest`가 포함됩니다:
- `samples/MewUI.Sample/app.manifest`

내부적으로 레이아웃은 DIP 기준이며, 그래픽 백엔드에서 디바이스 픽셀로 변환/스냅 처리하여 1px border를 또렷하게 그립니다.

## 라이선스

MIT. `LICENSE` 참고.
