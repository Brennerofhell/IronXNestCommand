Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Application]::EnableVisualStyles()

# ==========================================
# CLAUDE DESIGN FARB-PALETTE
# ==========================================
$cBg         = [System.Drawing.Color]::FromArgb(24,  24,  27)   # #18181B Dark Graphite
$cCard       = [System.Drawing.Color]::FromArgb(37,  37,  41)   # #252529 Card Surface
$cTerracotta = [System.Drawing.Color]::FromArgb(217, 119, 87)   # #D97757 Terracotta
$cText       = [System.Drawing.Color]::FromArgb(250, 249, 245)  # #FAF9F5 Paper White
$cMuted      = [System.Drawing.Color]::FromArgb(161, 161, 170)  # #A1A1AA Sand Gray
$cEmerald    = [System.Drawing.Color]::FromArgb(16,  185, 129)  # #10B981 Emerald
$cDanger     = [System.Drawing.Color]::FromArgb(239, 68,  68)   # #EF4444 Red
$cBorder     = [System.Drawing.Color]::FromArgb(63,  63,  70)   # #3F3F46 Border
$cYellow     = [System.Drawing.Color]::FromArgb(234, 179, 8)    # #EAB308 Warning Yellow

$fontTitle  = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$fontHeader = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI",  9, [System.Drawing.FontStyle]::Regular)
$fontBold   = New-Object System.Drawing.Font("Segoe UI",  9, [System.Drawing.FontStyle]::Bold)
$fontMono   = New-Object System.Drawing.Font("Consolas",  8, [System.Drawing.FontStyle]::Regular)

# ==========================================
# PFAD-HELFER
# ==========================================
function Find-GamePath {
    $paths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator",
        "C:\Program Files\Steam\steamapps\common\Iron Nest Heavy Turret Simulator",
        "D:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator",
        "E:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator",
        "F:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator"
    )
    foreach ($p in $paths) {
        if (Test-Path "$p\Iron Nest Heavy Turret Simulator.exe") { return $p }
    }
    return ""
}

function Find-DotNet {
    $root = Split-Path -Parent $PSScriptRoot
    $candidates = @(
        (Join-Path $root   "tools\dotnet-sdk\dotnet.exe"),
        (Join-Path $PSScriptRoot "dotnet-sdk\dotnet.exe"),
        (Join-Path $env:LOCALAPPDATA "dotnet\dotnet.exe"),
        "C:\Program Files\dotnet\dotnet.exe",
        "dotnet"
    )
    foreach ($c in $candidates) {
        if ($c -eq "dotnet") {
            $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
            if ($cmd) { return $cmd.Source }
        } elseif (Test-Path $c) { return $c }
    }
    return $null
}

$script:repoRoot        = Split-Path -Parent $PSScriptRoot
$script:currentGamePath = Find-GamePath
$script:dotnetExe       = Find-DotNet

# ==========================================
# HAUPTFENSTER
# ==========================================
$form = New-Object System.Windows.Forms.Form
$form.Text          = "Iron Nest // Mod Manager"
$form.Size          = New-Object System.Drawing.Size(700, 640)
# Muss mindestens so hoch sein wie TabControl-Y (64) + TabControl-Hoehe (510) + Fensterrahmen/Titelleiste,
# sonst wird die Lösch-Button-Leiste am unteren Rand des Deinstallations-Tabs abgeschnitten.
$form.MinimumSize   = New-Object System.Drawing.Size(640, 640)
$form.StartPosition = "CenterScreen"
$form.BackColor     = $cBg
$form.ForeColor     = $cText
$form.FormBorderStyle = "Sizable"

# Terracotta Top-Linie (3 px)
$topAccent = New-Object System.Windows.Forms.Panel
$topAccent.Location  = New-Object System.Drawing.Point(0, 0)
$topAccent.Size      = New-Object System.Drawing.Size(3000, 3)
$topAccent.BackColor = $cTerracotta
$topAccent.Anchor    = "Top,Left,Right"
$form.Controls.Add($topAccent)

# Header
$headerPanel = New-Object System.Windows.Forms.Panel
$headerPanel.Location = New-Object System.Drawing.Point(20, 12)
$headerPanel.Size     = New-Object System.Drawing.Size(640, 46)
$headerPanel.Anchor   = "Top,Left,Right"

$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Text      = "IRON NEST // MOD MANAGER"
$lblTitle.Font      = $fontTitle
$lblTitle.ForeColor = $cTerracotta
$lblTitle.AutoSize  = $true
$lblTitle.Location  = New-Object System.Drawing.Point(0, 0)
$headerPanel.Controls.Add($lblTitle)

$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text      = "Mods verwalten, bauen und deployen - alles an einem Ort."
$lblSub.Font      = $fontNormal
$lblSub.ForeColor = $cMuted
$lblSub.AutoSize  = $true
$lblSub.Location  = New-Object System.Drawing.Point(0, 24)
$headerPanel.Controls.Add($lblSub)

$form.Controls.Add($headerPanel)

# ==========================================
# TAB CONTROL
# ==========================================
$tabControl = New-Object System.Windows.Forms.TabControl
$tabControl.Location    = New-Object System.Drawing.Point(12, 64)
$tabControl.Size        = New-Object System.Drawing.Size(664, 510)
$tabControl.Anchor      = "Top,Bottom,Left,Right"
$tabControl.Font        = $fontBold
$tabControl.BackColor   = $cBg

$form.Controls.Add($tabControl)

# ==========================================
# TAB 1: DEINSTALLATION
# ==========================================
$tabUninstall = New-Object System.Windows.Forms.TabPage
$tabUninstall.Text      = "  Deinstallation  "
$tabUninstall.BackColor = $cBg
$tabUninstall.ForeColor = $cText
$tabControl.TabPages.Add($tabUninstall)

# Pfad-Panel
# Dock statt Anchor+Location: Anchor-basierte Pixel-Positionierung war fuer den regulaeren
# Fenstergroessen-Bereich ausgelegt und schnitt beim Maximieren des Fensters die Buttons am
# unteren Rand ab (die Anker-Deltas passen nicht mehr, wenn das Fenster VIEL groesser wird als
# im Design). Dock berechnet die Position bei jeder Groesse automatisch korrekt.
$pathPanel = New-Object System.Windows.Forms.Panel
$pathPanel.Height    = 44
$pathPanel.BackColor = $cBg
$pathPanel.Dock      = "Top"

# Innerer Karten-Streifen (behaelt das urspruengliche Card-Aussehen bei fester Hoehe bei)
$pathCard = New-Object System.Windows.Forms.Panel
$pathCard.Location  = New-Object System.Drawing.Point(8, 8)
$pathCard.Size      = New-Object System.Drawing.Size(636, 34)
$pathCard.BackColor = $cCard
$pathCard.Anchor    = "Top,Left,Right"
$pathPanel.Controls.Add($pathCard)

$lblPath = New-Object System.Windows.Forms.Label
$lblPath.Text      = "Spielordner:"
$lblPath.Font      = $fontBold
$lblPath.ForeColor = $cText
$lblPath.Location  = New-Object System.Drawing.Point(8, 7)
$lblPath.AutoSize  = $true
$pathCard.Controls.Add($lblPath)

$txtPath = New-Object System.Windows.Forms.TextBox
$txtPath.Text        = $script:currentGamePath
$txtPath.Font        = $fontNormal
$txtPath.BackColor   = $cBg
$txtPath.ForeColor   = $cText
$txtPath.BorderStyle = "FixedSingle"
$txtPath.Location    = New-Object System.Drawing.Point(92, 6)
$txtPath.Size        = New-Object System.Drawing.Size(420, 22)
$txtPath.Anchor      = "Top,Left,Right"
$pathCard.Controls.Add($txtPath)

$btnBrowse = New-Object System.Windows.Forms.Button
$btnBrowse.Text                        = "Durchsuchen..."
$btnBrowse.Font                        = $fontNormal
$btnBrowse.BackColor                   = $cBorder
$btnBrowse.ForeColor                   = $cText
$btnBrowse.FlatStyle                   = "Flat"
$btnBrowse.FlatAppearance.BorderSize   = 0
$btnBrowse.Location                    = New-Object System.Drawing.Point(520, 5)
$btnBrowse.Size                        = New-Object System.Drawing.Size(108, 24)
$btnBrowse.Anchor                      = "Top,Right"
$btnBrowse.Add_Click({
    $fbd = New-Object System.Windows.Forms.FolderBrowserDialog
    $fbd.Description = "Waehle den Iron Nest Heavy Turret Simulator Spielordner:"
    if ($fbd.ShowDialog() -eq "OK") {
        $txtPath.Text = $fbd.SelectedPath
        $script:currentGamePath = $fbd.SelectedPath
        Refresh-ModList
    }
})
$pathCard.Controls.Add($btnBrowse)

# Footer (Status + Buttons) — eigenes Dock="Bottom"-Panel mit fester Hoehe, damit die Lösch-
# Buttons garantiert sichtbar bleiben, unabhaengig von der tatsaechlichen Fenstergroesse.
$uFooterPanel = New-Object System.Windows.Forms.Panel
$uFooterPanel.Height  = 78
$uFooterPanel.Dock    = "Bottom"

# Status
$statusStrip = New-Object System.Windows.Forms.Label
$statusStrip.Height    = 26
$statusStrip.Padding   = New-Object System.Windows.Forms.Padding(8, 6, 8, 0)
$statusStrip.Font      = $fontNormal
$statusStrip.ForeColor = $cMuted
$statusStrip.Text      = "Bereit zum Scannen."
$statusStrip.Dock      = "Top"
$uFooterPanel.Controls.Add($statusStrip)

# Buttons unten (Deinstall-Tab) — FlowLayoutPanel statt Anchor="Top,Right": ordnet Buttons
# selbst nebeneinander an, unabhaengig von der tatsaechlichen Fensterbreite. Die vorherige
# Anchor-basierte Rechtsausrichtung von btnDelete berechnete ihre Marge offenbar anhand einer
# veralteten/falschen Breite des (selbst schon dynamisch groesse aendernden) Eltern-Panels und
# der Button verschwand dadurch komplett aus dem sichtbaren Bereich.
$uBtnPanel = New-Object System.Windows.Forms.FlowLayoutPanel
# Dock="Fill" hier (statt "Top" + feste Hoehe) verursachte einen reproduzierbaren WinForms-Bug:
# ein FlowLayoutPanel mit Dock="Fill" verschachtelt in einem Dock="Bottom"-Panel (selbst in einer
# TabPage/TabControl) zeichnet seine Kind-Buttons zwar mit korrekter Position/Farbe, aber OHNE
# Text. Isoliert reproduziert und verifiziert: Dock="Top" mit expliziter Hoehe behebt es zuverlaessig.
$uBtnPanel.Dock          = "Top"
$uBtnPanel.Height        = 44
$uBtnPanel.FlowDirection = "LeftToRight"
$uBtnPanel.WrapContents  = $false
$uBtnPanel.Padding       = New-Object System.Windows.Forms.Padding(8, 4, 8, 8)

function New-FooterBtn ($text, $w, $color, $fgColor, $font) {
    $b = New-Object System.Windows.Forms.Button
    $b.Text                      = $text
    $b.Font                      = $font
    $b.BackColor                 = $color
    $b.ForeColor                 = $fgColor
    $b.UseVisualStyleBackColor   = $false
    $b.TextAlign                 = "MiddleCenter"
    $b.AutoSize                  = $false
    $b.FlatStyle                 = "Flat"
    $b.FlatAppearance.BorderSize = 0
    $b.Size                      = New-Object System.Drawing.Size($w, 32)
    $b.Margin                    = New-Object System.Windows.Forms.Padding(0, 0, 8, 0)
    return $b
}

$btnRefresh = New-FooterBtn "Neu scannen" 120 $cBorder $cText $fontBold
$btnRefresh.Add_Click({ Refresh-ModList })
$uBtnPanel.Controls.Add($btnRefresh)

$btnOpenFolder = New-FooterBtn "Ordner oeffnen" 128 $cBorder $cText $fontNormal
$btnOpenFolder.Add_Click({
    if (Test-Path $script:currentGamePath) {
        Invoke-Item $script:currentGamePath
    } else {
        [System.Windows.Forms.MessageBox]::Show("Spielordner existiert nicht!", "Fehler", "OK", "Error")
    }
})
$uBtnPanel.Controls.Add($btnOpenFolder)

$btnDelete = New-FooterBtn "Ausgewaehlte Mods loeschen" 296 $cTerracotta ([System.Drawing.Color]::White) $fontBold
$btnDelete.Add_Click({ Delete-SelectedMods })
$uBtnPanel.Controls.Add($btnDelete)
$uFooterPanel.Controls.Add($uBtnPanel)

# ListView — Dock="Fill" nimmt automatisch den gesamten Platz zwischen pathPanel (Top) und
# uFooterPanel (Bottom) ein, bei jeder Fenstergroesse. Reihenfolge wichtig: Top/Bottom-Docks
# zuerst hinzufuegen, Fill-Control zuletzt.
$listView = New-Object System.Windows.Forms.ListView
$listView.View        = "Details"
$listView.CheckBoxes  = $true
$listView.FullRowSelect = $true
$listView.GridLines   = $true
$listView.BackColor   = $cCard
$listView.ForeColor   = $cText
$listView.Font        = $fontNormal
$listView.BorderStyle = "FixedSingle"
$listView.Dock        = "Fill"

$listView.Columns.Add("Mod / Datei",   240) | Out-Null
$listView.Columns.Add("Typ",           110) | Out-Null
$listView.Columns.Add("Groesse",        80) | Out-Null
$listView.Columns.Add("Relativer Pfad",185) | Out-Null

$tabUninstall.Controls.Add($pathPanel)
$tabUninstall.Controls.Add($uFooterPanel)
$tabUninstall.Controls.Add($listView)

# ==========================================
# TAB 2: BUILD & DEPLOY
# ==========================================
$tabBuild = New-Object System.Windows.Forms.TabPage
$tabBuild.Text      = "  Build + Deploy  "
$tabBuild.BackColor = $cBg
$tabBuild.ForeColor = $cText
$tabControl.TabPages.Add($tabBuild)

# --- dotnet-Status-Zeile ---
$dotnetPanel = New-Object System.Windows.Forms.Panel
$dotnetPanel.Location  = New-Object System.Drawing.Point(8, 10)
$dotnetPanel.Size      = New-Object System.Drawing.Size(636, 30)
$dotnetPanel.BackColor = $cCard
$dotnetPanel.Anchor    = "Top,Left,Right"

$lblDotnet = New-Object System.Windows.Forms.Label
$lblDotnet.Font     = $fontBold
$lblDotnet.ForeColor = $cText
$lblDotnet.Location = New-Object System.Drawing.Point(8, 6)
$lblDotnet.AutoSize = $true
if ($script:dotnetExe) {
    $lblDotnet.Text      = "dotnet:  $script:dotnetExe"
    $lblDotnet.ForeColor = $cEmerald
} else {
    $lblDotnet.Text      = "dotnet:  NICHT GEFUNDEN - bitte .NET SDK installieren"
    $lblDotnet.ForeColor = $cDanger
}
$dotnetPanel.Controls.Add($lblDotnet)
$tabBuild.Controls.Add($dotnetPanel)

# --- Repo-Root-Zeile ---
$repoPanel = New-Object System.Windows.Forms.Panel
$repoPanel.Location  = New-Object System.Drawing.Point(8, 44)
$repoPanel.Size      = New-Object System.Drawing.Size(636, 30)
$repoPanel.BackColor = $cCard
$repoPanel.Anchor    = "Top,Left,Right"

$lblRepoLbl = New-Object System.Windows.Forms.Label
$lblRepoLbl.Text      = "Repo:"
$lblRepoLbl.Font      = $fontBold
$lblRepoLbl.ForeColor = $cText
$lblRepoLbl.Location  = New-Object System.Drawing.Point(8, 7)
$lblRepoLbl.AutoSize  = $true
$repoPanel.Controls.Add($lblRepoLbl)

$lblRepoVal = New-Object System.Windows.Forms.Label
$lblRepoVal.Text      = $script:repoRoot
$lblRepoVal.Font      = $fontNormal
$lblRepoVal.ForeColor = $cMuted
$lblRepoVal.Location  = New-Object System.Drawing.Point(50, 7)
$lblRepoVal.AutoSize  = $true
$repoPanel.Controls.Add($lblRepoVal)
$tabBuild.Controls.Add($repoPanel)

# --- Build-Buttons ---
$buildBtnPanel = New-Object System.Windows.Forms.Panel
$buildBtnPanel.Location  = New-Object System.Drawing.Point(8, 82)
$buildBtnPanel.Size      = New-Object System.Drawing.Size(636, 44)
$buildBtnPanel.Anchor    = "Top,Left,Right"

function New-BuildBtn ($text, $x, $w, $color, $fgColor) {
    $b = New-Object System.Windows.Forms.Button
    $b.Text                      = $text
    $b.Font                      = $fontBold
    $b.BackColor                 = $color
    $b.ForeColor                 = $fgColor
    $b.FlatStyle                 = "Flat"
    $b.FlatAppearance.BorderSize = 0
    $b.Location                  = New-Object System.Drawing.Point($x, 5)
    $b.Size                      = New-Object System.Drawing.Size($w, 32)
    return $b
}

$btnBuildOnly = New-BuildBtn "Build (Release)" 0 148 $cBorder $cText
$btnBuildOnly.Add_Click({ Invoke-Build -mode "build" })
$buildBtnPanel.Controls.Add($btnBuildOnly)

$btnDeployBep = New-BuildBtn "Deploy BepInEx" 156 148 $cBorder $cText
$btnDeployBep.Add_Click({ Invoke-Build -mode "bepinex" })
$buildBtnPanel.Controls.Add($btnDeployBep)

$btnDeployMelon = New-BuildBtn "Deploy MelonLoader" 312 160 $cBorder $cText
$btnDeployMelon.Add_Click({ Invoke-Build -mode "melon" })
$buildBtnPanel.Controls.Add($btnDeployMelon)

$btnDeployBoth = New-BuildBtn "Build + Deploy BEIDE" 480 152 $cTerracotta ([System.Drawing.Color]::White)
$btnDeployBoth.Add_Click({ Invoke-Build -mode "both" })
$buildBtnPanel.Controls.Add($btnDeployBoth)

$tabBuild.Controls.Add($buildBtnPanel)

# --- Log-Output ---
$lblLog = New-Object System.Windows.Forms.Label
$lblLog.Text      = "Build-Ausgabe:"
$lblLog.Font      = $fontBold
$lblLog.ForeColor = $cMuted
$lblLog.Location  = New-Object System.Drawing.Point(8, 132)
$lblLog.AutoSize  = $true
$tabBuild.Controls.Add($lblLog)

$txtLog = New-Object System.Windows.Forms.RichTextBox
$txtLog.Location    = New-Object System.Drawing.Point(8, 152)
$txtLog.Size        = New-Object System.Drawing.Size(636, 280)
$txtLog.BackColor   = [System.Drawing.Color]::FromArgb(14, 14, 16)
$txtLog.ForeColor   = $cText
$txtLog.Font        = $fontMono
$txtLog.BorderStyle = "FixedSingle"
$txtLog.ReadOnly    = $true
$txtLog.ScrollBars  = "Vertical"
$txtLog.Anchor      = "Top,Bottom,Left,Right"
$tabBuild.Controls.Add($txtLog)

# --- Build-Status-Leiste ---
$buildStatus = New-Object System.Windows.Forms.Label
$buildStatus.Location  = New-Object System.Drawing.Point(8, 438)
$buildStatus.Size      = New-Object System.Drawing.Size(636, 22)
$buildStatus.Font      = $fontBold
$buildStatus.ForeColor = $cMuted
$buildStatus.Text      = "Bereit."
$buildStatus.Anchor    = "Bottom,Left,Right"
$tabBuild.Controls.Add($buildStatus)

# --- Clear-Button ---
$btnClearLog = New-Object System.Windows.Forms.Button
$btnClearLog.Text                      = "Log leeren"
$btnClearLog.Font                      = $fontNormal
$btnClearLog.BackColor                 = $cBorder
$btnClearLog.ForeColor                 = $cText
$btnClearLog.FlatStyle                 = "Flat"
$btnClearLog.FlatAppearance.BorderSize = 0
$btnClearLog.Location                  = New-Object System.Drawing.Point(8, 462)
$btnClearLog.Size                      = New-Object System.Drawing.Size(100, 26)
$btnClearLog.Anchor                    = "Bottom,Left"
$btnClearLog.Add_Click({ $txtLog.Clear(); $buildStatus.Text = "Bereit."; $buildStatus.ForeColor = $cMuted })
$tabBuild.Controls.Add($btnClearLog)

# ==========================================
# LOGIK: DEINSTALLATION
# ==========================================
function Refresh-ModList {
    $listView.Items.Clear()
    $script:currentGamePath = $txtPath.Text.Trim()

    if (-not (Test-Path "$script:currentGamePath\Iron Nest Heavy Turret Simulator.exe")) {
        $statusStrip.Text      = "Keine gueltige 'Iron Nest Heavy Turret Simulator.exe' im Pfad gefunden."
        $statusStrip.ForeColor = $cDanger
        return
    }

    $found = 0

    # Erkennt unsere eigene Mod unabhaengig vom konkreten Dateinamen-Muster, damit sie sich
    # in der Liste von fremden Plugins/Abhaengigkeiten (z.B. LiteNetLib.dll, SharpGLTF.Core.dll,
    # die zu einem Co-op-Plugin gehoeren koennen) klar unterscheidet.
    function Get-ModTypeLabel ($fileName, $defaultLabel) {
        if ($fileName -like "IronXNestCommand*") { return "IronXNestCommand (eigene Mod)" }
        if ($fileName -like "*Coop*") { return "Co-op Plugin (fremd)" }
        return $defaultLabel
    }

    # Fuer Ordner reicht die Namensprüfung allein nicht — z.B. "Mods\Mods\", "Mods\UserLibs\"
    # heissen nicht selbst "*Coop*", enthalten aber OpenNestCoop.MelonMod.dll/LiteNetLib.dll/etc.
    # Ohne diesen Inhalts-Check wuerde die "(fremd)"-Warnung bei genau den Ordnern fehlen, in
    # denen ein falsch entpacktes Co-op-Plugin am ehesten steckt.
    function Get-FolderTypeLabel ($dirInfo, $defaultLabel) {
        if ($dirInfo.Name -like "IronXNestCommand*") { return "IronXNestCommand (eigene Mod)" }
        if ($dirInfo.Name -like "*Coop*") { return "Co-op Plugin (fremd)" }
        $coopContent = Get-ChildItem $dirInfo.FullName -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "*Coop*" -or $_.Name -like "LiteNetLib*" -or $_.Name -like "SharpGLTF*" }
        if ($coopContent) { return "Enthaelt Co-op Plugin (fremd)" }
        return $defaultLabel
    }

    # BepInEx/plugins/
    $pluginsDir = Join-Path $script:currentGamePath "BepInEx\plugins"
    if (Test-Path $pluginsDir) {
        Get-ChildItem $pluginsDir -File -Filter "*.dll" | ForEach-Object {
            $type = Get-ModTypeLabel $_.Name "BepInEx Plugin"
            $item = New-Object System.Windows.Forms.ListViewItem($_.Name)
            $item.SubItems.Add($type) | Out-Null
            $item.SubItems.Add("$([math]::Round($_.Length/1KB,1)) KB") | Out-Null
            $item.SubItems.Add("BepInEx\plugins\$($_.Name)") | Out-Null
            $item.Tag = $_.FullName
            $listView.Items.Add($item) | Out-Null
            $found++
        }
        Get-ChildItem $pluginsDir -Directory | ForEach-Object {
            $sz = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
            $type = Get-FolderTypeLabel $_ "Plugin Ordner"
            $item = New-Object System.Windows.Forms.ListViewItem("$($_.Name) (Ordner)")
            $item.SubItems.Add($type) | Out-Null
            $item.SubItems.Add("$([math]::Round($sz/1KB,1)) KB") | Out-Null
            $item.SubItems.Add("BepInEx\plugins\$($_.Name)\") | Out-Null
            $item.Tag = $_.FullName
            $listView.Items.Add($item) | Out-Null
            $found++
        }
    }

    # Mods/ (MelonLoader) — inkl. Unterordner, manche Mods legen dort eigene Ordner an
    $modsDir = Join-Path $script:currentGamePath "Mods"
    if (Test-Path $modsDir) {
        Get-ChildItem $modsDir -File -Filter "*.dll" | ForEach-Object {
            $type = Get-ModTypeLabel $_.Name "MelonLoader Mod"
            $item = New-Object System.Windows.Forms.ListViewItem($_.Name)
            $item.SubItems.Add($type) | Out-Null
            $item.SubItems.Add("$([math]::Round($_.Length/1KB,1)) KB") | Out-Null
            $item.SubItems.Add("Mods\$($_.Name)") | Out-Null
            $item.Tag = $_.FullName
            $listView.Items.Add($item) | Out-Null
            $found++
        }
        Get-ChildItem $modsDir -Directory | ForEach-Object {
            $sz = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
            $type = Get-FolderTypeLabel $_ "MelonLoader Mod-Ordner"
            $item = New-Object System.Windows.Forms.ListViewItem("$($_.Name) (Ordner)")
            $item.SubItems.Add($type) | Out-Null
            $item.SubItems.Add("$([math]::Round($sz/1KB,1)) KB") | Out-Null
            $item.SubItems.Add("Mods\$($_.Name)\") | Out-Null
            $item.Tag = $_.FullName
            $listView.Items.Add($item) | Out-Null
            $found++
        }
    }

    # UserData/
    $userDataDir = Join-Path $script:currentGamePath "UserData"
    if (Test-Path $userDataDir) {
        Get-ChildItem $userDataDir -Directory | ForEach-Object {
            $sz = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
            $item = New-Object System.Windows.Forms.ListViewItem("$($_.Name) (Config)")
            $item.SubItems.Add("Mod Einstellungen") | Out-Null
            $item.SubItems.Add("$([math]::Round($sz/1KB,1)) KB") | Out-Null
            $item.SubItems.Add("UserData\$($_.Name)\") | Out-Null
            $item.Tag = $_.FullName
            $listView.Items.Add($item) | Out-Null
            $found++
        }
    }

    if ($found -eq 0) {
        $statusStrip.Text      = "Keine installierten Mods gefunden. Das Spielverzeichnis ist sauber."
        $statusStrip.ForeColor = $cEmerald
    } else {
        $statusStrip.Text      = "$found Mod-Element(e) im Spielverzeichnis erkannt."
        $statusStrip.ForeColor = $cText
    }
}

function Delete-SelectedMods {
    $checked = $listView.CheckedItems
    if ($checked.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show(
            "Bitte waehle mindestens eine Mod mit der Checkbox aus!", "Hinweis", "OK", "Information")
        return
    }
    $names        = ($checked | ForEach-Object { $_.Text }) -join "`n - "
    # Erkennt Co-op-Plugins unabhaengig vom genauen Namen (IronNestCoop, OpenNestCoop, etc.) sowie
    # deren typische Abhaengigkeiten, damit die Warnung nicht an einer hartcodierten Namensvariante
    # vorbeigeht, falls ein anderes/neueres Co-op-Plugin installiert ist.
    $coopSelected = $checked | Where-Object { $_.Text -like "*Coop*" -or $_.Text -like "LiteNetLib*" -or $_.Text -like "SharpGLTF*" -or $_.SubItems[1].Text -like "*Coop*" }
    $warningText  = "Moechtest du folgende Mod-Elemente wirklich loeschen?`n`n - $names"
    if ($coopSelected) {
        $warningText += "`n`nACHTUNG: Darunter ist mutmasslich das Co-op-Plugin oder eine seiner Abhaengigkeiten! Ohne diese Datei(en) funktioniert Multiplayer moeglicherweise nicht mehr."
    }
    $res = [System.Windows.Forms.MessageBox]::Show($warningText, "Deinstallation bestaetigen", "YesNo", "Warning")
    if ($res -eq "Yes") {
        $del = 0
        foreach ($item in $checked) {
            try {
                if (Test-Path $item.Tag) {
                    if ((Get-Item $item.Tag) -is [System.IO.DirectoryInfo]) {
                        Remove-Item -Recurse -Force $item.Tag
                    } else {
                        Remove-Item -Force $item.Tag
                    }
                    $del++
                }
            } catch {
                [System.Windows.Forms.MessageBox]::Show(
                    "Fehler beim Loeschen von $($item.Tag):`n$($_.Exception.Message)", "Fehler", "OK", "Error")
            }
        }
        [System.Windows.Forms.MessageBox]::Show(
            "$del Mod-Element(e) wurden erfolgreich geloescht!", "Erfolg", "OK", "Information")
        Refresh-ModList
    }
}

# ==========================================
# LOGIK: BUILD & DEPLOY
# ==========================================
function Append-Log ($text, $color = $null) {
    if ($color) {
        $txtLog.SelectionColor = $color
    } else {
        $txtLog.SelectionColor = $cText
    }
    $txtLog.AppendText("$text`n")
    $txtLog.ScrollToCaret()
    [System.Windows.Forms.Application]::DoEvents()
}

function Set-BuildButtons ($enabled) {
    $btnBuildOnly.Enabled   = $enabled
    $btnDeployBep.Enabled   = $enabled
    $btnDeployMelon.Enabled = $enabled
    $btnDeployBoth.Enabled  = $enabled
}

function Invoke-Build {
    param([string]$mode)

    if (-not $script:dotnetExe) {
        [System.Windows.Forms.MessageBox]::Show(
            "dotnet.exe nicht gefunden.`nBitte .NET SDK installieren: https://aka.ms/dotnet/8.0/dotnet-sdk-win-x64.exe",
            "Fehler", "OK", "Error")
        return
    }

    $slnPath   = Join-Path $script:repoRoot "IronXNestCommand.sln"
    $bepOutDir = Join-Path $script:repoRoot "IronXNestCommand.Host.BepInEx\bin\Release"
    $coreOut   = Join-Path $script:repoRoot "IronXNestCommand.Core\bin\Release"
    $melonOut  = Join-Path $script:repoRoot "IronXNestCommand.MelonLoader\bin\Release"

    $gamePath  = $txtPath.Text.Trim()
    if (-not $gamePath) { $gamePath = $script:currentGamePath }

    Set-BuildButtons $false
    $buildStatus.Text      = "Lauft..."
    $buildStatus.ForeColor = $cYellow

    Append-Log "======================================================" $cMuted
    Append-Log " Mode: $mode  |  $(Get-Date -Format 'HH:mm:ss')" $cMuted
    Append-Log "======================================================" $cMuted

    # --- BUILD ---
    Append-Log "" 
    Append-Log "[1] dotnet build $slnPath -c Release" $cTerracotta

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName               = $script:dotnetExe
    $psi.Arguments              = "build `"$slnPath`" -c Release"
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true
    $psi.UseShellExecute        = $false
    $psi.CreateNoWindow         = $true
    $psi.WorkingDirectory       = $script:repoRoot

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    $script:buildOk = $true

    $proc.Add_OutputDataReceived({
        param($s, $e)
        if ($null -ne $e.Data) {
            $line = $e.Data
            $col  = $cText
            if ($line -match "error")   { $col = $cDanger;  $script:buildOk = $false }
            if ($line -match "warning") { $col = $cYellow }
            if ($line -match "Build succeeded") { $col = $cEmerald }
            $form.Invoke([Action]{ Append-Log $line $col })
        }
    })
    $proc.Add_ErrorDataReceived({
        param($s, $e)
        if ($null -ne $e.Data) {
            $form.Invoke([Action]{ Append-Log $e.Data $cDanger })
            $script:buildOk = $false
        }
    })

    $proc.Start()         | Out-Null
    $proc.BeginOutputReadLine()
    $proc.BeginErrorReadLine()
    $proc.WaitForExit()

    if (-not $script:buildOk -or $proc.ExitCode -ne 0) {
        Append-Log "" 
        Append-Log "BUILD FEHLGESCHLAGEN (ExitCode $($proc.ExitCode))" $cDanger
        $buildStatus.Text      = "Build fehlgeschlagen!"
        $buildStatus.ForeColor = $cDanger
        Set-BuildButtons $true
        return
    }

    Append-Log ""
    Append-Log "Build erfolgreich." $cEmerald

    # --- DEPLOY ---
    # $script:deployErrors statt einer lokalen Variable, weil Copy-WithLog als verschachtelte
    # Funktion sonst eine eigene, nie gelesene Kopie erhoehen wuerde (PowerShell-Scoping) —
    # das Tool hat dadurch bisher IMMER "Alles erfolgreich" gemeldet, selbst wenn ein Copy fehlschlug.
    $script:deployErrors = 0

    function Copy-WithLog ($src, $dest, $label) {
        if (Test-Path $src) {
            try {
                $destDir = Split-Path $dest -Parent
                if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
                Copy-Item $src $dest -Force
                Append-Log "  [OK] $label" $cEmerald
            } catch {
                Append-Log "  [FEHLER] $label : $($_.Exception.Message)" $cDanger
                $script:deployErrors++
            }
        } else {
            Append-Log "  [WARN] Nicht gefunden: $src" $cYellow
        }
    }

    if ($mode -eq "bepinex" -or $mode -eq "both") {
        if (-not $gamePath -or -not (Test-Path $gamePath)) {
            Append-Log "[WARN] Spielordner nicht gesetzt/gefunden — BepInEx-Deploy uebersprungen." $cYellow
        } else {
            $pluginsDest = Join-Path $gamePath "BepInEx\plugins"
            Append-Log ""
            Append-Log "[2] Deploy BepInEx -> $pluginsDest" $cTerracotta
            Copy-WithLog (Join-Path $bepOutDir "IronXNestCommand.dll")      (Join-Path $pluginsDest "IronXNestCommand.dll")      "IronXNestCommand.dll (BepInEx)"
            Copy-WithLog (Join-Path $coreOut   "IronXNestCommand.Core.dll") (Join-Path $pluginsDest "IronXNestCommand.Core.dll") "IronXNestCommand.Core.dll"
        }
    }

    if ($mode -eq "melon" -or $mode -eq "both") {
        if (-not $gamePath -or -not (Test-Path $gamePath)) {
            Append-Log "[WARN] Spielordner nicht gesetzt/gefunden — MelonLoader-Deploy uebersprungen." $cYellow
        } else {
            $modsDest = Join-Path $gamePath "Mods"
            Append-Log ""
            Append-Log "[$(if ($mode -eq 'both') {'3'} else {'2'})] Deploy MelonLoader -> $modsDest" $cTerracotta

            # MelonLoader Ausgabepfad (net6.0 subdir oder direkt)
            $melonDll = Join-Path $melonOut "net6.0\IronXNestCommand.dll"
            if (-not (Test-Path $melonDll)) { $melonDll = Join-Path $melonOut "IronXNestCommand.dll" }

            Copy-WithLog $melonDll (Join-Path $modsDest "IronXNestCommand.dll") "IronXNestCommand.dll (MelonLoader)"
        }
    }

    Append-Log ""
    if ($script:deployErrors -gt 0) {
        Append-Log "Fertig mit $script:deployErrors Deploy-Fehler(n)." $cYellow
        $buildStatus.Text      = "Deploy abgeschlossen - $script:deployErrors Fehler."
        $buildStatus.ForeColor = $cYellow
    } else {
        Append-Log "Alles erfolgreich abgeschlossen!" $cEmerald
        $buildStatus.Text      = "Erfolgreich - $(Get-Date -Format 'HH:mm:ss')"
        $buildStatus.ForeColor = $cEmerald

        if ($mode -ne "build") {
            Refresh-ModList
        }
    }

    Set-BuildButtons $true
}

# ==========================================
# START
# ==========================================
Refresh-ModList
[void]$form.ShowDialog()
