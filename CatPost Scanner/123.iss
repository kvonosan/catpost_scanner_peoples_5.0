


;------------------------------------------------------------------------------
;
;       Пример установочного скрипта для Inno Setup 5.5.5
;       (c) maisvendoo, 15.04.2015
;
;------------------------------------------------------------------------------

;------------------------------------------------------------------------------
;   Определяем некоторые константы
;------------------------------------------------------------------------------

; Имя приложения
#define   Name       "CatPost Scanner"
; Версия приложения
#define   Version    "4.1"
; Фирма-разработчик
#define   Publisher  "Viktor Nasonov"
; Сафт фирмы разработчика
#define   URL        "https://www.tomnolane.ru"
; Имя исполняемого модуля
#define   ExeName    "catpost_scanner.exe"

;------------------------------------------------------------------------------
;   Устанавливаем языки для процесса установки
;------------------------------------------------------------------------------
[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl";

;------------------------------------------------------------------------------
;   Параметры установки
;------------------------------------------------------------------------------
[Setup]

; Уникальный идентификатор приложения, 
;сгенерированный через Tools -> Generate GUID
AppId={{200DFD0A-B9CF-431C-BA6C-8578F429B58D}

; Прочая информация, отображаемая при установке
AppName={#Name}
AppVersion={#Version}
AppPublisher={#Publisher}
AppPublisherURL={#URL}
AppSupportURL={#URL}
AppUpdatesURL={#URL}

; Путь установки по-умолчанию
DefaultDirName={pf}\{#Name}
; Имя группы в меню "Пуск"
DefaultGroupName={#Name}

; Каталог, куда будет записан собранный setup и имя исполняемого файла
OutputDir=D:\work
OutputBaseFileName=CatPost_Scanner-install

; Файл иконки
SetupIconFile=D:\work\catpostnew.png.ico

; Параметры сжатия
Compression=lzma
SolidCompression=yes

UninstallDisplayIcon={app}\{#ExeName}
;------------------------------------------------------------------------------
;   Опционально - некоторые задачи, которые надо выполнить при установке
;------------------------------------------------------------------------------
[Tasks]
; Создание иконки на рабочем столе
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

;------------------------------------------------------------------------------
;   Файлы, которые надо включить в пакет установщика
;------------------------------------------------------------------------------
[Files]

; Исполняемый файл
Source: "C:\Users\Виктор\Desktop\Catpost Scanner\catpost_scanner.exe"; DestDir: "{app}"; Flags: ignoreversion

; Прилагающиеся ресурсы
Source: "C:\Users\Виктор\Desktop\Catpost Scanner\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs;

[Dirs]
Name: {app}; Permissions: users-full

;------------------------------------------------------------------------------
;   Указываем установщику, где он должен взять иконки
;------------------------------------------------------------------------------ 
[Icons]

Name: "{group}\{#Name}"; Filename: "{app}\{#ExeName}"

Name: "{commondesktop}\{#Name}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Code]
function IsDotNetDetected(version: string; release: cardinal): boolean;

var 
    reg_key: string; // Ïðîñìàòðèâàåìûé ïîäðàçäåë ñèñòåìíîãî ðååñòðà
    success: boolean; // Ôëàã íàëè÷èÿ çàïðàøèâàåìîé âåðñèè .NET
    release45: cardinal; // Íîìåð ðåëèçà äëÿ âåðñèè 4.5.x
    key_value: cardinal; // Ïðî÷èòàííîå èç ðååñòðà çíà÷åíèå êëþ÷à
    sub_key: string;

begin

    success := false;
    reg_key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\';
    
    // Âðåñèÿ 3.0
    if Pos('v3.0', version) = 1 then
      begin
          sub_key := 'v3.0';
          reg_key := reg_key + sub_key;
          success := RegQueryDWordValue(HKLM, reg_key, 'InstallSuccess', key_value);
          success := success and (key_value = 1);
      end;

    // Âðåñèÿ 3.5
    if Pos('v3.5', version) = 1 then
      begin
          sub_key := 'v3.5';
          reg_key := reg_key + sub_key;
          success := RegQueryDWordValue(HKLM, reg_key, 'Install', key_value);
          success := success and (key_value = 1);
      end;

     // Âðåñèÿ 4.0 êëèåíòñêèé ïðîôèëü
     if Pos('v4.0 Client Profile', version) = 1 then
      begin
          sub_key := 'v4\Client';
          reg_key := reg_key + sub_key;
          success := RegQueryDWordValue(HKLM, reg_key, 'Install', key_value);
          success := success and (key_value = 1);
      end;

     // Âðåñèÿ 4.0 ðàñøèðåííûé ïðîôèëü
     if Pos('v4.0 Full Profile', version) = 1 then
      begin
          sub_key := 'v4\Full';
          reg_key := reg_key + sub_key;
          success := RegQueryDWordValue(HKLM, reg_key, 'Install', key_value);
          success := success and (key_value = 1);
      end;

     // Âðåñèÿ 4.5
     if Pos('v4.5', version) = 1 then
      begin
          sub_key := 'v4\Full';
          reg_key := reg_key + sub_key;
          success := RegQueryDWordValue(HKLM, reg_key, 'Release', release45);
          success := success and (release45 >= release);
      end;
        
    result := success;

end;

//-----------------------------------------------------------------------------
//  Ôóíêöèÿ-îáåðòêà äëÿ äåòåêòèðîâàíèÿ êîíêðåòíîé íóæíîé íàì âåðñèè
//-----------------------------------------------------------------------------
function IsRequiredDotNetDetected(): boolean;
begin
    result := IsDotNetDetected('v4.0 Full Profile', 0);
end;

//-----------------------------------------------------------------------------
//    Callback-ôóíêöèÿ, âûçûâàåìàÿ ïðè èíèöèàëèçàöèè óñòàíîâêè
//-----------------------------------------------------------------------------
function InitializeSetup(): boolean;
begin

  // Åñëè íåò òåðáóåìîé âåðñèè .NET âûâîäèì ñîîáùåíèå î òîì, ÷òî èíñòàëëÿòîð
  // ïîïûòàåòñÿ óñòàíîâèòü å¸ íà äàííûé êîìïüþòåð
  if not IsDotNetDetected('v4.0 Full Profile', 0) then
    begin
      MsgBox('{#Name} requires Microsoft .NET Framework 4.0 Full Profile.'#13#13
             'The installer will attempt to install it', mbInformation, MB_OK);
    end;   

  result := true;
end;

[Run]
;------------------------------------------------------------------------------
;   Ñåêöèÿ çàïóñêà ïîñëå èíñòàëëÿöèè
;------------------------------------------------------------------------------
Filename: {tmp}\dotNetFx40_Full_x86_x64.exe; Parameters: "/q:a /c:""install /l /q"""; Check: not IsRequiredDotNetDetected; StatusMsg: Microsoft Framework 4.0 is installed. Please wait...
