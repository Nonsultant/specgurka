# language: sv

# OWASP V14.4 HTTP Security Headers
@L1
@L2
@L3
Egenskap: Http Security Headers
    Som en säkerhetstestare vill jag verifiera att applikationen använder lämpliga HTTP-säkerhetsrubriker
    för att förhindra attacker som t.ex. Cross-Site Scripting (XSS).
    
    Läs mer om OWASP Application Security Verification Standard 4.0.3 - V14.4:
    [Link](https://owasp.org/www-project-application-security-verification-standard/)

    @L1
    @L2
    @L3
    Regel: OWASP 14.4.1 - Verifiera att varje HTTP-svar innehåller en Content-Type-header.
        Specificera även en säker teckenuppsättning (t.ex. UTF-8, ISO-8859-1) om innehållstypen är text/*, /+xml och
        application/xml. Innehållet måste matcha den angivna Content-Type-rubriken.
        
        Läs mer om Content-Type-headers på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Type)

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera teckenuppsättning för olika innehållstyper
            När jag skickar en GET-förfrågan till "<path>"
            Så  ska svaret innehålla en Content-Type-rubrik med innehållstyp "<innehållstyp>"
            Och om innehållstypen är av texttyp ska den innehålla en säker teckenuppsättning

            Exempel:
                | path                           | innehållstyp     |
                | /api/core/persons              | application/json |
                | /api/core/persons/ext/10000043 | application/json |

    @Ignore
    @L1
    @L2
    @L3
    Regel: OWASP 14.4.2 - Verifiera att alla API-svar innehåller en Content-Disposition-header.
        Content-Disposition ska vara satt till "attachment" med ett lämpligt filnamn för innehållstypen.
        Relevant vid endpoints som returnerar filer som ska laddas ner.
        
        Läs mer om Content-Disposition-headers på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Disposition)

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera Content-Disposition med lämpligt filnamn
            När jag skickar en GET-förfrågan till "<path>"
            Så  ska svaret innehålla en Content-Disposition-rubrik
            Och Content-Disposition-rubriken ska vara satt till "attachment"
            Och Content-Disposition-rubriken ska innehålla ett lämpligt filnamn för innehållstypen "<innehållstyp>"

            Exempel:
                | path                       | innehållstyp     |
                | /api/core/path/to/download | application/json |

    @L1
    @L2
    @L3
    Regel: OWASP 14.4.3 - Verifiera att en Content Security Policy (CSP) header finns på plats.
        CSP-headern ska hjälpa till att minska påverkan från XSS-attacker som HTML, DOM, JSON och JavaScript-injiceringar.
        
        Läs mer om Content Security Policy på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy)

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera CSP-skydd för olika innehållstyper
            När jag skickar en GET-förfrågan till "<path>"
            Så  ska svaret innehålla en Content-Security-Policy-rubrik
            Och ska svaret innehålla en Content-Type-rubrik med innehållstyp "<innehållstyp>"
            Och ska CSP-rubriken innehålla restriktioner för "script-src"
            Och ska CSP-rubriken innehålla restriktioner för "style-src"
            Och ska CSP-rubriken inte innehålla "unsafe-inline" för script
            Och ska CSP-rubriken inte innehålla "unsafe-eval" för script
            Och ska CSP-rubriken innehålla en rapporteringsmekanism
            Och ska minst ett av följande vara sant i CSP-rubriken för script:
                | alternativ                |
                | Innehåller nonce-värden   |
                | Innehåller hash-värden    |
                | Inga unsafe-inline-värden |

            Exempel:
                | path              | innehållstyp     |
                | /api/core/persons | application/json |

    @L1
    @L2
    @L3
    Regel: OWASP 14.4.4 - Verifiera att alla svar innehåller en X-Content-Type-Options: nosniff header.
        X-Content-Type-Options förhindrar webbläsare från MIME-sniffing vilket kan leda till säkerhetsproblem.
        
        Läs mer om X-Content-Type-Options på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options)

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera X-Content-Type-Options för olika innehållstyper
            När jag skickar en GET-förfrågan till "<path>"
            Så  ska svaret innehålla en X-Content-Type-Options-rubrik
            Och X-Content-Type-Options-rubriken ska vara satt till "nosniff"
            Och ska svaret innehålla en Content-Type-rubrik med innehållstyp "<innehållstyp>"

            Exempel:
                | path              | innehållstyp     |
                | /api/core/persons | application/json |

    @L1
    @L2
    @L3
    Regel: OWASP 14.4.5 - Verifiera att en Strict-Transport-Security (HSTS) header finns på alla svar.
        HSTS-headern ska inkludera max-age med tillräcklig varaktighet och includeSubdomains för alla underdomäner.
        
        Läs mer om Strict-Transport-Security på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Strict-Transport-Security)

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera Strict-Transport-Security för olika innehållstyper
            När jag skickar en GET-förfrågan till "<path>"
            Så  ska svaret innehålla en Strict-Transport-Security-rubrik
            Och Strict-Transport-Security-rubriken ska innehålla "max-age" med värde minst 15724800
            Och Strict-Transport-Security-rubriken ska inkludera "includeSubdomains"
            Och ska svaret innehålla en Content-Type-rubrik med innehållstyp "<innehållstyp>"
            Och ska Strict-Transport-Security-rubriken inkludera "preload"

            Exempel:
                | path              | innehållstyp     |
                | /api/core/persons | application/json |

    @L1
    @L2
    @L3
    Regel: OWASP 14.4.6 - Verifiera att en lämplig Referrer-Policy header är inkluderad.
        Referrer-Policy förhindrar att känslig information i URL:er exponeras via Referer-headern till otillförlitliga parter.
        
        Läs mer om Referrer-Policy på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy)

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera Referrer-Policy för olika innehållstyper
            När jag skickar en GET-förfrågan till "<path>"
            Så  ska svaret innehålla en Referrer-Policy-rubrik
            Och Referrer-Policy-rubriken ska ha något av följande säkra värden:
                | no-referrer                     |
                | no-referrer-when-downgrade      |
                | same-origin                     |
                | strict-origin                   |
                | strict-origin-when-cross-origin |
                | origin-when-cross-origin        |
            Och Referrer-Policy-rubriken ska inte innehålla något av följande osäkra värden:
                | unsafe-url |
                | origin     |

            Exempel:
                | path              |
                | /api/core/persons |

    @ignore
    @L1
    @L2
    @L3
    Regel: OWASP 14.4.7 - Verifiera att innehållet i webbapplikationen inte kan bäddas in i tredjepartssidor som standard.
        Inbäddning av specifika resurser bör endast tillåtas där det är nödvändigt genom lämpliga Content-Security-Policy: frame-ancestors och X-Frame-Options rubriker.
        
        Läs mer om X-Frame-Options på MDN:
        [Link](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options)

        @L1
        @L2
        @L3
        Abstrakt Scenario: Verifiera clickjacking-skydd för olika innehållstyper
            När jag skickar en GET-förfrågan till "<path>"
            Så  ska svaret innehålla en X-Frame-Options-rubrik
            Och X-Frame-Options-rubriken ska ha ett av följande säkra värden:
                | säkert värde |
                | DENY         |
                | SAMEORIGIN   |
            Och ska svaret innehålla en Content-Type-rubrik med innehållstyp "<innehållstyp>"
            Och ska CSP-rubriken innehålla direktivet "frame-ancestors"
            Och ska CSP-direktivet "frame-ancestors" ha ett restriktivt värde som matchar X-Frame-Options

            Exempel:
                | path            | innehållstyp               |
                | /path/to/iframe | image, text/html, text/xml |
