# Information zu dem BLL "Xplorer"
Dieses Projekt ist entstanden durch meine Leidenschaft an Videospielen und Informatik, welche sich in eine Neugier zur eignene Videospielentwicklung wiederfanden. Inspiriert ist dieses Videospiel durch Spiele wie No Man's Sky, Minecraft und Skyrim. Es ist ein Projekt, welches mich herausfordern sollte die Prozesse hinter solchen Spielen zu verstehen und Erfahrungen in diesem Feld zu sammeln. 

## Inhalt
- [Aufbau](#aufbau-des-projekts)
- [Dokumentation](#dokumentation)
- [Quellen und AI-Nutzung](#quellen-und-ai-nutzung)

## Aufbau des Projekts
Den Ordner den dieses Git Repository darstellt ist der Asset Folder innerhalb eines Unity Projektes.

### BLL_Xplorer.exe
Dies ist die Datei des Spieles. Einfach runterladen, entpacken und auf einem Windows Rechner ausprobieren.
(Warnung: Manche Funktionen des Menus sind noch nicht fertig und verlangen eventuell eine Neustart des Spieles)
### Scripts
Hier sind alle Scripts gelagert welche das Spiel ausmachen.
#### Editor Scripts
Diese Scripts sind dazu dar den Inspector innerhalb von Unity zu gestalten und neue Funktionen hinzuzufügen, die bei der Entwicklung des Spieles helfen.
#### Generation Logic
Diese Scripts enthalten den ganze Code für das generieren der Welt um den Spieler herum.
#### Player Scripts
Diese Scripts steueren den Spieler und seine Kamera, wie auch die Position in der Relation zu Welt.
#### Weiter Scripts
Diese Scripts erstellen/steuern weiter Teile des Videospiels sind aber keiner Kategorie zuzuordnen.
### Scenes
Diese Dateien enthalten den Aufbau der verschiedenen Welten die genutzt werden um das Spiel zusammenzubauen.
### Prefabs
Diese Datein sind vorgefertigte Objekte welche im Spiel instanziieren werden.
### Misc_Data
Dieser Ordner enthält verschiedene Datein die helfen das Spiel zu bauen.

## Dokumentation
Die Dokumentation dieses Projektes finden sie in der Datei "Project Documentation BLL Oskar Trillitzsch.pdf" und eine kurze Erklärung zu den verschiedenen Klassen finden sie in "Generation Logic Short Explanation.pdf"

## Quellen und AI-Nutzung
Das Projekt stütz sich auf mehrere Quellen, besonders Tutorials auf Youtube, die Unity Dokumentation und AI (ChatGPT und Github Copilot). Alle Codeschnipsel die komplett mit AI generiert worden sind, sind innerhalb der einzelnen Klassen makiert. Es kann jedoch sein, dass manchen Codeschnipsel die in AI-Gesprächen und meinem Code aufkommen nicht makiert sind, die lag häufig daran, dass ich schon Prozesse implimentiert hatte, welche die AI erst später vorgeschlagen hat. Es gibt auch Schnipsel, die erstmals von AI generiert worden sind, dann jedoch von Grund auf neu geschreiben worden sind, aber dennoch dem Orginalen ähnlich sehen. Solche neu geschreibenen oder stark modifizierten Codeschnispel sind nicht als AI markiert.

### Youtube
- [Tutorial Serie von Brackeys](https://www.youtube.com/watch?v=eJEpeUH1EMg&list=PLrMEhC9sAD1zprGu_lphl3cQSS3uFIXA9) (zuletzt aufgerufen 26.03.2026)
- [Tutorial Serie von Sebastian Lague](https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3) (zuletzt aufgerufen 26.03.2026)
- [Tutorial Serie von DVS Devs](https://www.youtube.com/watch?v=1qSjCu8av7Q&list=PLnRmXj9gwMNdJNPUHu0x6kTZYy_YzRZP0) (zuletzt aufgerufen 26.03.2026)

### Unity Dokumentation
- [Unitys Dokumentation](https://docs.unity3d.com/ScriptReference/) (zuletzt aufgerufen 26.03.2026)

### Andere Quellen
- [Red Blob Games](https://www.redblobgames.com/maps/terrain-from-noise/) (zuletzt aufgerufen 26.03.2026)

### AI-Chats
- [Getting started with Unity](https://chatgpt.com/share/69c53ea4-a6d0-8329-bda1-eba5d49b3daa)
- [Perlin Noise C# Code](https://chatgpt.com/share/69c53efa-922c-832f-b5ee-2dee22659ad4)
- [Perlin Noise Generation](https://chatgpt.com/share/69c53f38-d130-8327-b56d-1f7467dc1065)
- [Terrain Chunk Generation](https://chatgpt.com/share/69c53f55-00e8-8331-89e9-415048153d05)
- [Circular Generation Logic](https://chatgpt.com/share/69c53f7c-4130-832c-905c-4b10b6c15830)
- [Mesh Collider Setup](https://chatgpt.com/share/69c53f9d-b1d4-832f-b665-67a62877bdb0)
- [Enum Switch Case](https://chatgpt.com/share/69c53fb1-0c70-8326-be7b-68085114e202)
- [Prefab Secene Refrence Issue](https://chatgpt.com/share/69c53fc9-f084-8327-924e-719177ecd505)
- [Fix Perlin Noise Scaling](https://chatgpt.com/share/69c53ff9-7de4-8330-9467-8d290c92bab8)
- [Branch - Fix Perlin Noise Scaling](https://chatgpt.com/share/69c54023-3760-8326-b97e-bae42c455743)
- [Vector Mapping for Noise](https://chatgpt.com/share/69c54044-52d0-8326-96f9-fbc130f8e5ad)
- [Unity Terrain Shader issue](https://chatgpt.com/share/69c54060-f75c-8331-a29e-a74a10eeff03)
- [Unity Material Blending](https://chatgpt.com/share/69c5407e-0180-832d-983e-5c63c9a34c48)
- [Branch - Unity Material Blending](https://chatgpt.com/share/69c54092-7bf4-8327-8f25-56aaa8191dd5)
- [Branch - Branch - Unity Material Blending](https://chatgpt.com/share/69c540b9-37d8-8329-a502-5da70a908d18)
- [Unity Input System Error](https://chatgpt.com/share/69c540d9-7bf8-8332-8bd7-950d17dbd8b6)
- [Multitreading Unity Generation](https://chatgpt.com/share/69c54101-a5c0-8329-b0f0-01933d68ab33)
- [Code Debugging Help](https://chatgpt.com/share/69c5412c-f0c8-8329-b5e6-8d05ee99458c)
- [Unity Performance Optimization](https://chatgpt.com/share/69c54142-cb2c-8333-8a7a-662e4bb75f70)
- [Tree Spawn Issue Fix](https://chatgpt.com/share/69c52b57-9760-832c-8b05-a4362050e5b4)

Nun das Problem, was man mit den AI Chats innerhalb von VIsual Studio Code hat. Sie sind leider nicht gut exportierbar. Diese Chats können nur als unleserliche JSON Datei abgespeichert werden, deswegen hab ich mir von ChatGPT ein HTML-Script generieren lassen, der diese Dateien auslesen und wiedergeben kann. Um diese also einzusehen muss man:
- Die Datei chatviewer.html mit einem Browser öffnen
- In das Feld für JSON Dateien die gewünschte Datei aus VSCode_AI_Chats auswählen
- Damit hat man eine relativ gut leserliche Darstellung dieser Dateien

