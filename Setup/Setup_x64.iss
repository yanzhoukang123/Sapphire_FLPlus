; -- setup.iss --
;
; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Sapphire Capture Software"
#define MyAppVersion "1.0.0.1130"
#define MyAppPublisher "Azure Biosystems, Inc."
#define MyCompany "Azure Biosystems"
#define MyAppURL "http://www.azurebiosystems.com/"
#define MyAppExeName "Azure.LaserScanner.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{0545CFE6-05DD-45E9-A995-13566C431591}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf64}\{#MyCompany}\Sapphire
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=EULA.rtf
OutputBaseFilename=Sapphire_setup
Compression=lzma
SolidCompression=yes
DisableWelcomePage=no
DisableDirPage=no
OutputDir=.
PrivilegesRequired=admin
WindowVisible=yes
WindowShowCaption=no

[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Tasks]
Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
; Intel IPP
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\*; DestDir: {app}\Ipp; Flags: ignoreversion recursesubdirs createallsubdirs
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\em64t\ippcoreem64t-6.0.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\em64t\ippcvmx-6.0.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\em64t\ippimx-6.0.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\em64t\libiomp5md.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\ia32\ippcore-6.0.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\ia32\ippcvpx-6.0.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\ia32\ippipx-6.0.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Ipp\ia32\libiomp5md.dll; DestDir: {app}; Flags: ignoreversion
; AForge.NET Framework
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\AForge.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\AForge.Imaging.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\AForge.Math.dll; DestDir: {app}; Flags: ignoreversion
; AvalonDock
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\AvalonDock.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\AvalonDock.Themes.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\AvalonDock.xml; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\AvalonDockMVVM.dll; DestDir: {app}; Flags: ignoreversion
; Azure sapphire capture application
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.Adorners.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.CameraLib.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.CommandLib.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.GalilMotor.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.Image.Processing.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.ImagingSystem.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.Interfaces.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.IppImaging.NET.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.LaserScanner.exe; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.LaserScanner.exe.config; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.Resources.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.SettingsManager.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.UpdateConfig.exe; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Azure.WPF.Framework.dll; DestDir: {app}; Flags: ignoreversion
; Copy config.xml to dest app folder and temp folder (to be edit base on installation options selected).
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\config.xml; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\config.xml; DestDir: {tmp}; Flags: dontcopy
; rename existing config.xml ProgramData\Azure Biosystems\Sapphire\ to config.old
; Source: "{commonappdata}\Azure Biosystems\Sapphire\config.xml"; DestDir: "{commonappdata}\Azure Biosystems\Sapphire"; DestName: "config.old"; Flags: external skipifsourcedoesntexist
; Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\config.xml; DestDir: "{commonappdata}\Azure Biosystems\Sapphire"; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\CyUSB.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\DynamicDataDisplay.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\DynamicDataDisplay.xml; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Hats.APDCom.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Method.xml; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\PVCam.NET.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Sapphire User Manual.pdf; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Secure.xml; DestDir: {app}; Flags: ignoreversion
; Copy SysSettings.xml to dest app folder and temp folder (to be edit base on installation options selected).
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\SysSettings.xml; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\SysSettings.xml; DestDir: {tmp}; Flags: dontcopy
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\System.Windows.Interactivity.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\TaskDialog.dll; DestDir: {app}; Flags: ignoreversion
;Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\TaskDialog.xml; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\ToggleSwitch.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Utilities.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\WPFFolderBrowser.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Projects\Azure.LaserScanner\Build\x64\Release\Xceed.Wpf.Toolkit.dll; DestDir: {app}; Flags: ignoreversion

Source: Drivers\*; DestDir: {app}\Drivers; Flags: ignoreversion recursesubdirs createallsubdirs
;Source: Simulation\*; DestDir: {app}\Simulation; Flags: ignoreversion recursesubdirs createallsubdirs
;Source: Utilities\*; DestDir: {app}\Utilities; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

; Engineering GUI
Source: ..\Projects\Azure.ScannerEUI\Build\x64\Release\Azure.ScannerEUI.exe; DestDir: {app}; Flags: ignoreversion

[Icons]
Name: {group}\{#MyAppName}; Filename: {app}\{#MyAppExeName}
Name: {group}\{cm:UninstallProgram,{#MyAppName}}; Filename: {uninstallexe}
Name: {commondesktop}\{#MyAppName}; Filename: {app}\{#MyAppExeName}
;Name: {commonstartup}\{#MyAppName}; Filename: {app}\{#MyAppExeName}
Name: {userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}; Filename: {app}\{#MyAppExeName}; Tasks: quicklaunchicon

[Registry]
;Root: HKLM; Subkey: "SOFTWARE\Microsoft\TabletTip\1.7"; ValueType: dword; ValueName: "EnableDesktopModeAutoInvoke"; ValueData: "00000001"

[Run]
Filename: {app}\Drivers\CP210x_Windows_Drivers\CP210xVCPInstaller_x64.exe; Check: IsWin64; Flags: waituntilterminated; Tasks: ; Languages: 
;Filename: {app}\Drivers\CP210x_Windows_Drivers\CP210xVCPInstaller_x86.exe; Check: not IsWin64; Flags: waituntilterminated; Tasks: ; Languages: 
Filename: {app}\Drivers\Cypress_FX3\Win10\x64\DPInst.exe; Check: IsWin64; Flags: waituntilterminated;
;Filename: {app}\Drivers\Cypress_FX3\Win10\x86\DPInst.exe; Check: not IsWin64; Flags: waituntilterminated;
Filename: {app}\Drivers\GalilTools-1.6.4.580-Win-x64.exe; Flags: waituntilterminated;
Filename: {app}\Drivers\PVCAM-3-6-7-2-Setup.exe; Flags: waituntilterminated;
Filename: {pf64}\Photometrics\PVCam\utilities\PowerStates\PowerStatesCLI.exe; Parameters: 0; Flags: waituntilterminated skipifdoesntexist;
Filename: {app}\{#MyAppExeName}; Description: {cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}; Flags: nowait postinstall skipifsilent

[Code]
var
  InstallationType: string;
  InstallationModule: string;
  InstallationTypePage: TInputOptionWizardPage;
  ModuleSelectionPage: TInputOptionWizardPage;

procedure RenameConfigXmlFile;
var
  ConfigXmlFile: string;
  ConfigXmlFileRenamed: string;
  SysSettingsXmlFile: string;
  SysSettingsXmlFileRenamed: string;
begin
  ConfigXmlFile := ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\config.xml');
  ConfigXmlFileRenamed :=  ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\config.old');
  SysSettingsXmlFile := ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml');
  SysSettingsXmlFileRenamed :=  ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.old');

  // delete config.old if exists
  if FileExists(ConfigXmlFileRenamed) then
  begin
    DeleteFile(ConfigXmlFileRenamed);
  end
  // rename config.xml to config.old
  if FileExists(ConfigXmlFile) then
  begin
    RenameFile(ConfigXmlFile, ConfigXmlFileRenamed);
  end

  // delete SysSettings.old if exists
  if FileExists(SysSettingsXmlFileRenamed) then
  begin
    DeleteFile(SysSettingsXmlFileRenamed);
  end
  // rename SysSettings.xml to SysSettings.old
  if FileExists(SysSettingsXmlFile) then
  begin
    FileCopy(SysSettingsXmlFile, SysSettingsXmlFileRenamed, false);
  end
end;

function UpdateConfigXml(InstallationType, InstallationModule: string): Boolean;
var
  ResultCode: Integer;
  execParam: String;
  AppDataFolder : string;
  ConfigPath: string;
  SysSettingsPath : string;
begin
  // Extract config.xml and config file updater to temp folder
  ExtractTemporaryFile('Config.xml');
  ExtractTemporaryFile('Azure.UpdateConfig.exe');

  ConfigPath := ExpandConstant('{tmp}\Config.xml');

  if (FileExists(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml'))) then
  begin
    SysSettingsPath := ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml')
  end
  else begin
    ExtractTemporaryFile('SysSettings.xml');
    SysSettingsPath := ExpandConstant('{tmp}\SysSettings.xml')
  end
  // Launch Azure.UpdateConfig.exe to update the 'config.xml' and 'SysSettings.xml' and wait for it to terminate
  execParam := InstallationType + ' ' + InstallationModule + ' ' + '"' + ConfigPath + '"' + ' ' + '"' + SysSettingsPath + '"';
  if Exec(ExpandConstant('{tmp}\Azure.UpdateConfig.exe'), execParam, '', SW_SHOW,
     ewWaitUntilTerminated, ResultCode) then
  begin
    // handle success if necessary; ResultCode contains the exit code

    // copy modified 'config.xml' file from temp folder to Program folder
    result := FileCopy(ExpandConstant('{tmp}\config.xml'), ExpandConstant('{app}\Sapphire\config.xml'), false);

    if (FileExists(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml'))) then
    begin
      // copy modified 'SysSettings.xml' file from Programdata folder to Program folder
      result := FileCopy(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml'), ExpandConstant('{app}\SysSettings.xml'), false);  
    end
    else begin
      // copy modified 'SysSettings.xml' file from temp folder to app Program folder
      result := FileCopy(ExpandConstant('{tmp}\SysSettings.xml'), ExpandConstant('{app}\SysSettings.xml'), false);
    end

    // copy modified 'config.xml' file from temp folder to app data folder
    AppDataFolder := ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire');
    if (DirExists(AppDataFolder)) then
    begin
      // copy modified 'config.xml' file from temp folder to common application data folder
      result := FileCopy(ExpandConstant('{tmp}\config.xml'), ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\config.xml'), false);
    end
  end
  else begin
    // handle failure if necessary; ResultCode contains the error code
    result := false;
  end;
end;

function InitializeSetup: Boolean;
Begin
  RenameConfigXmlFile();
  Result := True;
end;

procedure InitializeWizard;
begin
  InstallationType := '';
  InstallationModule := '';

  InstallationTypePage := CreateInputOptionPage(wpWelcome,
    'Sapphire image capture software installation options', 'Select your installation option',
    'Please select the installation option, then click Next.',
    True, False);
  // Add items
  InstallationTypePage.Add('RGB');
  InstallationTypePage.Add('RGBNIR');
  InstallationTypePage.Add('NIR');
  // Set initial value
  InstallationTypePage.SelectedValueIndex := 0;

  // Create the Sapphire module selection page
  ModuleSelectionPage := CreateInputOptionPage(InstallationTypePage.ID,
    'Sapphire image capture software installation options',
    'Select your Sapphire module',
    'Please select a module, then click Next.',
    True, False);
  ModuleSelectionPage.Add('Chemi');
  ModuleSelectionPage.Add('None');

  // Set default selection
  ModuleSelectionPage.SelectedValueIndex := 3;
end;

procedure CurPageChanged(CurPageID: Integer);
//var
  //DefaultInstallPath: String;
begin
  if CurPageID = ModuleSelectionPage.ID then
  begin
    case InstallationTypePage.SelectedValueIndex of
      0: begin
           InstallationType := 'RGB';
         end;
      1: begin
           InstallationType := 'RGBNIR';
         end;
      2: begin
           InstallationType := 'NIR';
         end;
    end;
  end else if CurPageID = wpLicense then
  begin
    case ModuleSelectionPage.SelectedValueIndex of
      0: begin
           InstallationModule := 'Chemi';
         end;
      1: begin
           InstallationModule := 'None';
         end;

    end;
    // update the config.xml in the temp folder
    //UpdateConfigXml(InstallationType, InstallationModule);
    //WizardForm.DirEdit.Text := DefaultInstallPath;
  end
  else if CurPageID = wpFinished then
  begin
    UpdateConfigXml(InstallationType, InstallationModule);

    // Copy the modified config file to the program folder
    //if (FileExists(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\config.xml'))) then
    //begin
    //  FileCopy(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\config.xml'), ExpandConstant('{app}\config.xml'), false);
    //end
    //else begin
    //  FileCopy(ExpandConstant('{tmp}\config.xml'), ExpandConstant('{app}\config.xml'), false);
    //end
    // Copy the modified syssettings file to the program folder
    //if (FileExists(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml'))) then
    //begin
    //  FileCopy(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml'), ExpandConstant('{app}\SysSettings.xml'), false);
    //end
    //else begin
    //  FileCopy(ExpandConstant('{commonappdata}\Azure Biosystems\Sapphire\SysSettings.xml'), ExpandConstant('{app}\SysSettings.xml'), false);
    //end
  end;
end;

