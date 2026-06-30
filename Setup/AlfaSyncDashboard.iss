; ============================================================
;  Alfa Sync Dashboard — Inno Setup Script
;  Requiere: Inno Setup 6.x  (https://jrsoftware.org/isinfo.php)
;
;  Ejecutar desde la carpeta Setup\ o desde el IDE de Inno Setup.
;  Antes de compilar, publicar la aplicacion en modo Release:
;
;    dotnet publish ..\AlfaSyncDashboard\AlfaSyncDashboard.csproj ^
;      -c Release -r win-x64 --self-contained true
;
; ============================================================

#define AppName      "Alfa Sync Dashboard"
#define AppVersion   "1.0"
#define AppPublisher "Alfa"
#define AppExeName   "AlfaSyncDashboard.exe"
#define BuildDir     "..\AlfaSyncDashboard\bin\Release\net8.0-windows\win-x64"

[Setup]
AppId={{F3A7B2C1-94D8-4E6F-A1B0-3C5D7E8F9012}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppContact=albertofavioantunez@gmail.com
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=Output
OutputBaseFilename=AlfaSyncDashboard_Setup_v{#AppVersion}
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Requiere Windows 64-bit (la app es self-contained win-x64)
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
; Requiere permisos de administrador para instalar en Program Files
PrivilegesRequired=admin
; Mostrar licencia, no mostrar "Ready to Install" (queda mas limpio)
DisableReadyPage=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
; --- Aplicacion (self-contained .NET 8, todos los archivos menos el appsettings de dev) ---
Source: "{#BuildDir}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "appsettings.json,*.pdb,createdump.exe"

; --- Configuracion limpia (sin connection string; el wizard la completa en el primer uso) ---
Source: "appsettings.setup.json"; \
  DestDir: "{app}"; \
  DestName: "appsettings.json"; \
  Flags: ignoreversion onlyifdoesntexist

[Icons]
; Menu Inicio
Name: "{group}\{#AppName}";          Filename: "{app}\{#AppExeName}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
; Escritorio (solo si el usuario marco la tarea)
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; Ofrecer iniciar la aplicacion al terminar el instalador
Filename: "{app}\{#AppExeName}"; \
  Description: "Iniciar {#AppName} ahora"; \
  Flags: nowait postinstall skipifsilent

[Code]
// Preguntar si conservar la configuracion al desinstalar
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    if FileExists(ExpandConstant('{app}\appsettings.json')) then
      if MsgBox(
        '¿Desea conservar el archivo de configuracion (appsettings.json)?' + #13#10 +
        'Si elige No, la configuracion de conexion sera eliminada.',
        mbConfirmation, MB_YESNO) = IDYES then
        // Mover a un lugar seguro antes de que el desinstalador lo borre
        RenameFile(
          ExpandConstant('{app}\appsettings.json'),
          ExpandConstant('{app}\appsettings.json.bak'));
end;

// Si una actualizacion previa dejo la configuracion respaldada, restaurarla.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigPath: string;
  BackupPath: string;
begin
  if CurStep = ssPostInstall then
  begin
    ConfigPath := ExpandConstant('{app}\appsettings.json');
    BackupPath := ExpandConstant('{app}\appsettings.json.bak');

    if (not FileExists(ConfigPath)) and FileExists(BackupPath) then
      RenameFile(BackupPath, ConfigPath);
  end;
end;
