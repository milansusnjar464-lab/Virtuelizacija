# Elektricni motor - WCF projekat

Projekat implementira client/server obradu merenja elektricnog motora preko WCF `net.tcp` servisa.

## Struktura

- `Server/Common` - modeli, fault contract-i, validator i WCF service contract.
- `Server/Server` - self-hosted WCF servis, cuvanje prihvacenih i odbijenih merenja.
- `Server/Client` - CSV citac i klijent koji salje do 100 validno parsiranih merenja.

## Pokretanje

1. Build:

   ```powershell
   dotnet build Server\Projekat.slnx
   ```

2. Pokrenuti server:

   ```powershell
   Server\Server\bin\Debug\Server.exe
   ```

3. U drugom terminalu pokrenuti klijent:

   ```powershell
   Server\Client\bin\Debug\Client.exe
   ```

## CSV ulaz

Klijent prvo trazi veliki dataset u `Server\Client\Dataset\measures_v2.csv`. Taj fajl je namerno ignorisan u git-u jer moze biti prevelik za push. Ako ga nema, koristi se `measures_demo.csv`, mali primer koji je ukljucen u projekat.

Podrzane kolone su iz Kaggle electric motor temperature dataseta, a za projekat se koriste:

- `i_q`
- `i_d`
- `coolant`
- `profile_id`
- `ambient`
- `torque`

Server cuva rezultate u `Server\Server\bin\Debug\Files`:

- `measurements_ELECTRIC_MOTOR_001.csv`
- `rejects_ELECTRIC_MOTOR_001.csv`
