# docs/

| Pfad | Inhalt |
|---|---|
| `IST-ZUSTAND.md` | Bestandsaufnahme 2026-07-30: was der Mod tut, Architektur, Befundliste |
| `diagrams/*.puml` | 9 PlantUML-Quellen (siehe Tabelle in `IST-ZUSTAND.md` §3) |
| `rendered/*.svg` | Generiert — nicht per Hand editieren |

## Diagramme neu rendern

In Rider: PlantUML-Plugin installieren, `.puml` öffnen, Preview-Panel.

Per CLI (Java 17 + Graphviz 2.44 sind auf dieser Maschine vorhanden):

```bash
JAR="C:/Users/TEute/AppData/Roaming/JetBrains/Rider2026.1-backup/2026-07-17-04-44/plugins/plantuml4idea/lib/plantuml-mit-1.2026.2.jar"
cd docs/diagrams
java -jar "$JAR" -tsvg -o ../rendered *.puml
```

`-tpng` statt `-tsvg` für PNG. Syntaxfehler landen als Fehlertext *im* Bild —
nach `grep -il "syntax error" ../rendered/*.svg` prüfen.
