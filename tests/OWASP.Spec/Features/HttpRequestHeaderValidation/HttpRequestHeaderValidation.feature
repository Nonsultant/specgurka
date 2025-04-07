# language: sv

# OWASP V14.5 HTTP Request Header Validation
@L1
@L2
@L3
@ejkomplett
Egenskap: HTTP Request Header Validation

    HTTP Request Header Validation (HTTP-begäranshuvudvalidering) är en kritisk säkerhetsåtgärd i OWASP Application Security Verification Standard (ASVS) (se punkt [V14.5](https://github.com/OWASP/ASVS/blob/v4.0.3/4.0/en/0x22-V14-Config.md#v145-http-request-header-validation)) 
    eftersom HTTP-huvuden används för att styra och hantera kommunikationen mellan klienter och servrar. Om dessa huvuden inte kontrolleras ordentligt kan en applikation bli sårbar för 
    flera typer av attacker som kan leda till dataintrång, sessionkapning eller systemexploatering.
    
    HTTP Request Header Validation innebär att webbapplikationen kontrollerar att informationen i inkommande förfrågningar är korrekt och säker. Detta hjälper till att förhindra 
    attacker där angripare försöker skicka skadliga eller manipulerade uppgifter, såsom förfalskade identiteter eller otillåtna kommandon. Genom att validera dessa uppgifter kan vi skydda användarnas 
    data och säkerställa att endast legitima förfrågningar behandlas.

    HTTP Request Header Validation innebär att applikationen (kan vara och bör vara båda på webbserver och applikations nivå) granskar och filtrerar inkommande HTTP-huvuden (headers) för att säkerställa att de 
    följer förväntade format och värden. Detta inkluderar kontroll av obligatoriska huvuden, begränsning av okända eller oväntade huvuden samt verifiering av värden för att motverka angrepp som HTTP 
    smuggling, host header poisoning och header injection. Genom att strikt validera och normalisera inkommande HTTP-huvuden kan applikationen förhindra otillbörlig åtkomst, manipulering av sessionsdata 
    och andra säkerhetsrisker relaterade till felaktigt hanterade HTTP-begäranden.
    Bakgrund: 
        Givet att detta är en Feature bakgrund
        När jag visar detta på VizGurka Feature
        Så ska detta visas som scenario steps i Feature.

    @L1
    @L2
    @L3
    Regel: OWASP 14.5.1 - Verifiera att applikationsservern endast accepterar giltiga HTTP-metoder.
        Verifiera att applikationsservern endast accepterar de HTTP-metoder som används av
        applikationen/API:et, inklusive pre-flight OPTIONS-förfrågningar, och loggar/varnar
        vid förfrågningar som inte är giltiga för applikationens kontext.
        
        Läs mer om HTTP-metoder på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods)

    Bakgrund: 
        Givet att detta är en Rule bakgrund
        När jag visar detta på VizGurka
        Så ska detta visas som scenario steps.

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera att endast giltiga HTTP-metoder accepteras för endpoints
            När jag skickar en "<metod>"-förfrågan till "<path>"
            Så  ska svaret ha statuskoden "<förväntad_status>"
            Och om metoden är ogiltig ska svaret innehålla headern "Allow" med de tillåtna metoderna
            Och om metoden är OPTIONS ska svaret innehålla CORS-headers för tillåtna metoder

            # Och   om metoden är ogiltig ska en säkerhetsvarning registreras i systemloggen
            # Och   om metoden är ogiltig ska loggposten innehålla information om förfrågningen
            # Ett krav är att applikationen ska logga varningar vid ogiltiga förfrågningar
            # Detta krav är inte implementerat i detta exempel då det kräver att testprojektet
            # har tillgång till systemloggen.
            Exempel:
                | metod   | path              | förväntad_status | kommentar           |
                | GET     | /api/core/persons | 200              | Giltig metod        |
                | POST    | /api/core/persons | 400              | Felaktig begäran    |
                | PUT     | /api/core/persons | 405              | Ogiltig metod       |
                | DELETE  | /api/core/persons | 405              | Ogiltig metod       |
                | OPTIONS | /api/core/persons | 405              | Ogiltig metod       |
                | PATCH   | /api/core/persons | 405              | Ogiltig metod       |
                | HEAD    | /api/core/persons | 405              | Ogiltig metod       |
                | TRACE   | /api/core/persons | 405              | Ogiltig metod       |
                | INVALID | /api/core/persons | 405              | Okänd/ogiltig metod |

    @ignore
    @L1
    @L2
    @L3
    # Detta scenario är ignorerat då det kräver att testprojektet har tillgång till serverkoden.
    # Att göra blackbox tester för detta kommer inte vara givande på något sätt.
    Regel: OWASP 14.5.2 - Verifiera att Origin-headern inte används för autentisering eller åtkomstkontroll
        Verifiera att den angivna Origin-headern inte används för autentisering eller
        åtkomstkontrollbeslut, eftersom Origin-headern enkelt kan ändras av en
        angripare.
        
        Läs mer om Origin-headern:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Origin)

    @L1
    @L2
    @L3
    Regel: OWASP 14.5.3 - Verifiera att CORS-header använder strikt lista med betrodda domäner
        Verifiera att Cross-Origin Resource Sharing (CORS) Access-Control-Allow-Origin header
        använder en strikt vitlista av betrodda domäner och underdomäner för att matcha mot
        och inte stödjer ursprunget "null".
        
        Läs mer om CORS och Access-Control-Allow-Origin:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Access-Control-Allow-Origin)

        @L1
        @L2
        @L3
        # Detta scenario antar att localhost:3002 är en betrodd domän
        Abstrakt Scenario: Verifiera att CORS-headers hanteras korrekt för cross-origin requests
            När jag skickar en "<metod>" förfrågan till "<path>" med Origin-header "<origin>"
            Så  om origin är betrodd ska svaret innehålla korrekt Access-Control-Allow-Origin header
            Och om origin är obetrodd ska svaret inte innehålla Access-Control-Allow-Origin header
            Och svarshuvudet Access-Control-Allow-Origin ska aldrig innehålla värdet "*" eller "null"

            Exempel:
                | metod | path              | origin                     |
                | GET   | /api/core/persons | https://localhost:3002     |
                | GET   | /api/core/persons | https://untrusted-site.com |
                | GET   | /api/core/persons | null                       |

    @ignore
    @L2
    @L3
    # Detta scenario är ignorerat då det kräver att testprojektet kan hämta en giltig bearer token
    # att skicka med i testets förfrågningar.
    # Testprojektet behöver även tillgång till serverkoden för att kontrollera säkerhetsloggen.
    Regel: OWASP 14.5.4 - Verifiera att HTTP-headers från betrodda proxys och SSO-enheter valideras
        Verifiera att HTTP-headers som läggs till av betrodda proxys eller SSO-enheter, såsom
        bearer tokens, valideras och autentiseras av applikationen för att förhindra
        identitetsförfalskning och session hijacking.
        
        Läs mer om HTTP-autentisering och säkerhet:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication)
        [Link](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

        @L2
        @L3
        Abstrakt Scenario: Verifiera att HTTP-headers från betrodda proxys och SSO-enheter valideras korrekt
            När jag skickar en "<metod>"-förfrågan till "<path>" med headern "<header>" som innehåller värdet "<värde>"
            Så  ska applikationen validera headers korrekt
            Och om headern är ogiltig eller manipulerad ska autentiseringen misslyckas
            Och om en ogiltig header detekteras ska en säkerhetslogg skapas

            Exempel:
                | metod | path | header | värde |
                | metod | path | header | värde |
