# Heart of NTI: Millennium Dawn

En submod till [Millennium Dawn](https://steamcommunity.com/sharedfiles/filedetails/?id=2777392649) för Hearts of Iron IV som byter porträtten mot bilder på kompisar och lärare på NTI. Det är samma länder och personer som i [Heart of NTI 4](https://github.com/miniskuk/heart-of-nti-4), men här byts **alla** porträtt för landet: ledare, generaler, amiraler, rådgivare och forskare.

> Alla personer på bilderna har sagt att det är okej att bilderna är publika.

## Innehåll

| Mapp | Vad det är |
|---|---|
| `mod/` | Själva modden. Kräver Millennium Dawn och ska ligga **efter** den i laddordningen. |
| `kallor/` | De beskurna porträtten från Heart of NTI 4 som modden byggs av (en mapp per land). |
| `verktyg/md_bygg.ps1` | Bygger om modden (PowerShell, inga extra program behövs). |
| `verktyg/Dds.cs` | Läser och skriver DDS-bilder, används av `md_bygg.ps1`. |

## Länder

| Land | Tag i MD | Vem |
|---|---|---|
| USA | USA | Donald Trump |
| Japan | JAP | Hatsune Miku |
| Italien | ITA | Nunzia Lampitelli |
| Finland | FIN | Casper |
| Sverige | SWE | Liam |
| Korea | KOR | Emma |
| Frankrike | FRA | Arnaud Dillinger |
| Ryssland | SOV | Timmy |
| Grekland | GRE | Georgios Arvanitidis |
| Turkiet | TUR | Deniz Özgurdamar |
| Iran | PER | Javad Maafi |
| Storbritannien | ENG | Viktor Tsouplakis |
| Danmark | DEN | Katarina Gladun Komaromi |
| Schweiz | SWI | Jonas Nyman |
| Norge | NRY | Anton |
| Portugal | POR | Lina Aker |
| Serbien | SER | Anna Hjalmarsson (var Jugoslavien i NTI 4) |
| Nederländerna | HOL | Anna Jeppsson |
| Belgien | BEL | Ida Pettersson |
| Albanien | ALB | Mathias Sandström |
| Österrike | AUS | Pelle Helander |
| Tjeckien | CZE | Per Björkil |
| Polen | POL | Marcus Persson |
| Kurdistan | KUR | Agiri |
| Indien | RAJ | Brave |
| Chile | CHL | Stefan Löfven |
| Venezuela | VEN | Markus Allard |
| Peru | PRU | Juntan av Två (samma bild som i NTI 4) |
| Uruguay | URG | Carterpcs |
| Brasilien | BRA | Generalen |
| Bolivia | BOL | Claude 🤖 |
| Ecuador | ECU | ChatGPT 🤖 |
| Colombia | COL | Gemini 🤖 (vilse) |
| Argentina | ARG | Katterna – olika katter på alla karaktärer |

## Bygga om

Kör efter en uppdatering av Millennium Dawn, eller om du bytt en bild i `kallor/`:

```powershell
powershell -ExecutionPolicy Bypass -File .\verktyg\md_bygg.ps1
powershell -ExecutionPolicy Bypass -File .\verktyg\md_bygg.ps1 -Lander SWE,USA   # bara några länder
```

Skriptet letar upp varje porträtt som Millennium Dawn har för landet (`gfx/leaders/<TAG>/` plus porträtt som landets karaktärer pekar på) och gör en ny bild i exakt samma storlek. Om Millennium Dawn ligger någon annanstans än i Steams standardmapp: lägg till `-MD "sökväg"`.

## Ladda upp till Workshop

Öppna launchern och välj **Mod Tools** → **Upload Mod**. Första gången skapas en ny Workshop-sida. Skriv sedan in det nya Workshop-ID:t som `remote_file_id` i `mod/descriptor.mod`, så uppdateras samma sida nästa gång.
