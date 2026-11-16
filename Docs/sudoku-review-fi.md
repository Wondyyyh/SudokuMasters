# SudokuMasters: generaattorin ja kontrollien arvio

Arvio 11.11.2026. Projektin Unity-versio on 6000.6.8f1. Generaattorin toimintaa ei muutettu tässä tarkastuksessa; kontrollien korostus- ja syöttövirheitä korjattiin.

## Nykytilanne mitattuna

`Tools/Inspect-SudokuGenerator.ps1` suorittaa varsinaisen SudokuGenerator.cs-tiedoston erillään Unitysta. Vain Unityn lokitus ja Vector2Int on korvattu pienillä vastineilla. Jokaiselle tasolle generoitiin 30 tehtävää toistettavilla siemenillä: 20261111 + taso * 10000 + otosnumero (0–29). Ratkaisujen määrä tarkistettiin erillisellä MRV-ratkaisijalla ja singles-ratkeavuus erillisellä, kaikki 27 aluetta kattavalla ratkaisijalla.

| Valittu taso | Yksikäsitteisiä | Ratkesi vain naked/hidden singleillä | Annettuja numeroita | Nykyiset pisteet |
|---|---:|---:|---:|---:|
| Easy | 30/30 | 30/30 | 41 | 2–6 |
| Medium | 30/30 | 30/30 | 29–40 | 4–43 |
| Hard | 30/30 | 7/30 | 23–26 | 22–120 |

Näissä otoksissa poistomäärä vastasi palautettua ruudukkoa. Kaikissa tehtävissä oli yksi ratkaisu. Tämä tukee generaattorin perusrakennetta, mutta ei todista kaikkien mahdollisten tehtävien laatua. Singles-ratkeavuus ei myöskään yksin mittaa ratkaisun työläyttä. Tulokset osoittavat silti, ettei medium nykyisin edellytä uusia ratkaisukeinoja easyyn verrattuna ja että hard hyväksyy myös singles-tehtäviä.

Rivien ja sarakkeiden vaihtaminen keskenään muutti nykyistä pistemäärää easyssä 9/30, mediumissa 21/30 ja hardissa 30/30 tapauksessa. Skannausjärjestys voi itsessään vaikuttaa askelmääriin; nykyisten rivikohtaisten ratkaisukeinojen ja eräkierrosten laskemisen vuoksi pistettä ei kuitenkaan voi pitää vakaana vaikeusmittarina.

## Generaattorin tärkeimmät ongelmat

1. **Piiloparin tunnistus on virheellinen.** `ApplyHiddenPairs` etsii kaksi solua, jotka sisältävät molemmat numerot, mutta ei tarkista, esiintyvätkö numerot muuallakin samassa alueessa. Tällöin poistot eivät ole perusteltuja. Oikeassa piiloparissa kumpikin numero rajoittuu samoihin kahteen soluun. Virhe koskee analysointia; yksikäsitteisyystarkistus on tästä erillinen.
2. **Ratkaisukeinojen kattavuus on epäsymmetrinen.** Hidden singles sekä naked/hidden pairs tutkivat vain rivejä. X-Wing kattaa vain rivisuunnan. Pointing löytyy molempiin suuntiin, mutta claiming puuttuu. Sama looginen tilanne voi siis saada eri käsittelyn sen suunnasta riippuen.
3. **Pisteet laskevat käsittelykierroksia.** Yksi `ApplyNakedSingles` voi täyttää monta solua, mutta kasvattaa laskuria vain yhdellä. Hidden singles ja eliminaatiot kärsivät vastaavasta ongelmasta. Raportti ei kerro todellista askelmäärää.
4. **Keskeneräinen analyysi voidaan hyväksyä hardiksi.** Jos looginen ratkaisu jää kesken, pisteisiin lisätään vain 50. Raportista puuttuu erillinen valmistumis- ja ristiriitatieto. Myös `IsSolved` tarkistaa vain nollien puuttumisen, ei lopullisen ruudukon oikeellisuutta. Tuntematon vaikeus ei ole sama kuin hard.
5. **Tasoilla on vain pistekatto.** Hard ei edellytä hard-tekniikkaa eikä medium medium-tekniikkaa. Poistettujen numeroiden määrä on lähinnä ulkoasun ja työmäärän säätö; se ei takaa tiettyä päättelytasoa.
6. **Poistohaku katkeaa ensimmäiseen liian vaikeaan ehdokkaaseen.** Yhden poiston epäonnistuminen ei tarkoita, etteivät muut poistot toimisi. `bestBoard` alkaa täydestä ratkaisusta: jos hyväksyttävää välitulosta ei synny, palautus voi antaa täyden ruudukon ja poistojen laskuri voi jäädä eri tilaan. Tätä reunatapausta ei havaittu 90 tehtävän otoksessa.
7. **Generointi suoritetaan pääsäikeellä.** `yield return null` ennen generointia ei jaa itse hakua usealle ruudunpäivitykselle. Vaikean tehtävän hakeminen voi pysäyttää käyttöliittymän. Mittauksen hard-osuus vei noin 41,6 sekuntia 30 tehtävälle; tämä on erillisen .NET-ajon kokonaisaika tarkistuksineen, ei Unityn tai puhelimen suorituskykymittaus.

## Ehdotus easy / medium / hard -tasojen sisällöksi

Nämä ovat pelille ehdotetut rajat, eivät yleispätevä sudokuasteikko. Rajat kannattaa viimeistellä harrastajien pelitestauksella.

| Taso | Sallitut ratkaisukeinot | Hyväksynnän vähimmäisehto |
|---|---|---|
| Easy | Naked singles ja hidden singles riveissä, sarakkeissa ja laatikoissa | Koko tehtävä ratkeaa näillä. Kohtuullisesti helposti löydettäviä aloituksia. |
| Medium | Edelliset + locked candidates (pointing ja claiming) sekä naked pairs kaikissa alueissa | Easy-ratkaisija jää jumiin, medium-ratkaisija ratkaisee tehtävän. |
| Hard | Edelliset + hidden pairs, naked triples ja tavallinen X-Wing kumpaankin suuntaan | Medium-ratkaisija jää jumiin, hard-ratkaisija ratkaisee tehtävän. |

Kaikilla tasoilla täsmälleen yksi ratkaisu ja kokonainen looginen ratkaisupolku ilman arvaamista. Hardin sisällä voi rajata vaikeiden askelten määrää, jotta mukana ei ole yhtä aikaa montaa raskasta pullonkaulaa. Ketjut, forcing-menetelmät ja muut äärimmäiset tekniikat jätetään myöhempään vaikeustasoon.

Pelkkä se, että ratkaisija *käytti* X-Wingiä, ei riitä hard-leimaan: sen on ensin pitänyt käyttää loppuun kaikki helpommat sallittavat keinot. Kunkin onnistuneen päätelmän jälkeen palataan helpoimpaan keinoon.

## Toteutusjärjestys

1. **Korjaa looginen ratkaisija ennen pisteiden säätöä.** Yhteinen 27 alueen esitys; yksi perusteltu sijoitus tai eliminaatio kerrallaan. Lisää piiloparille vastaesimerkkitestit ja jokaiselle tekniikalle rivi-, sarake- ja laatikkotestit soveltuvin osin. Yksikäsitteisyys ratkaistaan edelleen erillisellä täydellisellä haulla.
2. **Palauta selitettävä analyysiraportti.** `solved`, `contradiction`, `hardestTechnique`, todelliset sijoitus-/poistoaskelten määrät ja ratkaisupolku: tekniikka, alue, lähdesolut, numerot ja poistettavat ehdokkaat. Sama polku mahdollistaa myöhemmin asteittaiset vihjeet.
3. **Hyväksy tehtävä sen ratkeavuusprofiilin perusteella.** Generoi ratkaisu, poista numeroita yksikäsitteisyys säilyttäen, luokittele valmis tehtävä ja hyväksy vain pyydetty taso. Liian helppo tai ratkeamatta jäävä tehtävä hylätään tai sijoitetaan omaan asianmukaiseen jonoon. Älä palauta toista tasoa pyydetyllä nimellä.
4. **Optimoi vasta toimivan luokituksen päälle.** MRV ja bittimaskit ratkaisujen laskentaan, kaksi ratkaisua katkaisurajana, Fisher–Yates-sekoitus nykyisen vinoutuneen sekoituksen tilalle. Rajaa yritykset ja käytetty aika. Tarjoa etukäteen validoitu tehtävävarasto, jos uusi haku ei ehdi valmistua. Valinnainen 180 asteen annettujen numeroiden symmetria tuo viimeistellyn ulkoasun, mutta ei korvaa vaikeusarviointia.
5. **Erota tehtävän tiedot pelaajan tilanteesta.** Alkuperäinen puzzle, solution ja pelaajan grid omiksi kopioikseen. Nykyinen staattinen puzzle siirtyy pelilaudalle viitteenä ja muuttuu pelaajan mukana. Uusi peli kannattaa rakentaa vasta juuri sen oman generointituloksen valmistuttua; vanha ei-null staattinen puzzle ei ole valmistumissignaali.
6. **Kalibroi ja testaa erissä.** Satoja tai tuhansia siemeniä per taso: yksikäsitteisyys, oikea ratkaisu, täysi looginen ratkeavuus, vaikeuden ala-/yläraja, raportin ja ruudukon vastaavuus, toistettavuus, ristiriidattomat poistot sekä kesto myös kohdelaitteella. Vertaa osaa tehtävistä riippumattomaan ratkaisijaan ja harrastajien arvioihin. Tallenna siemen ja generaattorin versio virheiden toistamista varten.

## Tässä korjatut kontrollit

- Merkintävalinnan vihreä väri säilyy, vaikka numerokorostus tyhjennetään. Värin laskenta on välitön ja yhteinen; seuraavan ruudunpäivityksen korostus ei enää ylikirjoita valintaa.
- Haetun numeron sisältävät kulma- ja keskimerkintäsolut korostuvat vihreällä. Tavallinen yksittäinen valinta säilyy sinisenä. Merkintähaun osuma ei muuta solua merkintäsyöttötilaan.
- Shift + numero lisää merkinnän vain valittuun soluun tai oikealla hiirellä valittuun ryhmään. Aiemmin jokainen muokattava solu saattoi vastaanottaa merkinnän.
- Näppäimistön ylärivin numerot sidotaan Input Systemin oikeisiin näppäinpolkuihin (`<Keyboard>/1` jne.). Aiempi `digit1`-muoto ei vastaa projektin Keyboard-layoutin ohjainta, joten ylärivin syöttö ei välittynyt.
- Merkintöjen poisto päivittää korostukset. Poistuvat solut siivotaan staattisesta monivalinnasta. Tauko tyhjentää merkintävalinnan.
- Delete toimii Backspacen lisäksi. Molemmat Ctrl-näppäimet on sidottu Ctrl-tapahtumaan; aiemmin tapahtuma oli kytketty vahingossa Esc-painallukseen.
- InputActionsManager ottaa syötön uudelleen käyttöön komponentin palatessa aktiiviseksi. Liike lukee valmistuneen input-arvon ja päivittää pidetyn suunnan; aivan keskeneräiseen ruudukkoon siirtyminen ohitetaan.

Kontrollien tarkistus löytyy Unityn Play-tilassa valikosta **Tools > Sudoku > Run Control Checks**. Tarkistus käyttää oikeaa solukoodia ja projektin merkintäprefabeja erillisissä testisoluissa. Se tarkistaa myös Input Systemin numerosyötön, Shiftin, Deleten, oikean Ctrlin, nuolen sekä syötön uudelleenaktivoinnin.

Lopputulos: **17/17 tarkistusta läpi** Unity 6000.6.8f1:n Play-tilassa. Näppäimistötestit syöttävät tapahtumat suoraan Unity Input Systemiin. Hiiren solunvalinta ja vihreä merkintävalinta tarkistettiin myös pelinäkymässä. Työpöytäautomaation näppäinpainalluksilla merkinnän lisäystä ei saatu näkyviin, joten fyysisen näppäimistön koko syöttöketjun toimivuutta ei väitetä näiden perusteella todennetuksi. Generaattorin tarkistus on 90 tehtävän otos, ei kattava laatutakuu.

## Kontrollien seuraavat parannukset

1. **Undo/redo kaikille muutoksille**, myös monisolumerkinnöille yhtenä peruttavana toimintona. Oikeaksi todetun pelaajanumeron lukitseminen estää nyt muokkaamisen ja paljastaa samalla ratkaisun oikeellisuuden; tee ratkaisuntarkistuksesta valinnainen apu.
2. **Selvästi näkyvät Number / Corner / Centre -tilat** ja numeropainikkeet. Nykyiset hiiren oikea painike ja Shift eivät yksin palvele kosketusnäyttöä eikä niiden merkitys avaudu uudelle pelaajalle.
3. **Tauon ja voiton aikainen syötön esto.** Peittävän valikon avaaminen ei itsessään estä laudan globaaleja näppäintapahtumia. Escape voisi vaihtaa tauon ja jatkamisen välillä.
4. **Yhtenäinen syöttö laudan kautta**, jotta 81 solun erilliset numerotapahtumien kuuntelijat eivät hallitse samaa toimintoa toisistaan riippumatta. Monivalinnan merkintänumero lisätään kaikkiin valittuihin, jos se puuttuu yhdestäkin; muuten poistetaan kaikista. Nykyinen solukohtainen toggle voi kääntää sekoittuneen ryhmän päinvastaiseksi.
5. **Valinnaiset apuasetukset:** ristiriitojen näyttö sääntöjen perusteella, ratkaisua vasten tarkistaminen erikseen, automaattinen vertaismerkintöjen poisto ja rivin/sarakkeen/laatikon kevyt korostus. Anna harrastajan ratkaista myös ilman jatkuvaa palautetta.

## Lähteet

- [HoDoKu: Rating and difficulty levels](https://hodoku.sourceforge.net/en/docs_cre.php): vaikein ratkaisukeino ja ratkaisupolun työmäärä erillisinä arviointiperusteina; luokituksen rajat ovat säädettäviä.
- [HoDoKu: Hidden subsets](https://hodoku.sourceforge.net/en/tech_hidden.php): piiloparin numeroiden on rajoituttava samoihin kahteen soluun saman alueen sisällä.

Lähteet tukevat arviointiperiaatteita. Tässä esitetty kolmen vaikeustason jako on nimenomaan SudokuMastersille ehdotettu tuoteratkaisu.
