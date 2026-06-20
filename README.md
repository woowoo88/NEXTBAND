# NextBand

Aplicativo desktop desenvolvido em C# WPF para configuração de uma pulseira inteligente com NFC, Bluetooth, ESP32, LED RGB e display OLED.

## Funcionalidades

- Login e cadastro de usuário
- Dashboard com perfil e status da pulseira
- Compartilhamento de perfil via NFC
- Registro de conexões recentes
- Tela de conexões com busca
- Edição de perfil
- Configuração da pulseira NextBand
- Conexão Bluetooth simulada com ESP32
- Link rápido para Instagram, LinkedIn ou URL
- URL personalizada de emergência
- Página pública de emergência infantil
- Modo criança
- Controle de permissões
- Armazenamento em banco SQL Server compartilhado

## Tecnologias utilizadas

- C#
- WPF
- XAML
- MVVM
- SQL Server remoto/compartilhado
- Microsoft.Data.SqlClient
- Hash PBKDF2 com salt para senhas
- Serviços preparados para Bluetooth, NFC e ESP32

## Banco de dados

O app não armazena informações pessoais em JSON, TXT, XML ou banco local do computador. Todos os dados persistentes usam SQL Server via connection string configurada em:

```powershell
$env:NEXTBAND_SQL_CONNECTION="Server=SEU_SERVIDOR;Database=NextBand;User Id=SEU_USUARIO;Password=SUA_SENHA;TrustServerCertificate=True;"
```

Todos os computadores que usarem a mesma connection string acessam o mesmo banco.

Na primeira execução, o aplicativo cria automaticamente as tabelas necessárias:

- `Users`
- `UserProfiles`
- `BandDevices`
- `Connections`
- `EmergencyProfiles`
- `EmergencyContacts`
- `AdditionalInformation`
- `AppSettings`

As senhas são salvas apenas como `PasswordHash` e `PasswordSalt`.

## Como executar

1. Configurar `NEXTBAND_SQL_CONNECTION`.
2. Abrir o projeto no Visual Studio.
3. Restaurar dependências.
4. Compilar e executar.

Ou pelo terminal:

```bash
dotnet build
dotnet run
```

## Estrutura do projeto

- `Views`: telas XAML do aplicativo.
- `ViewModels`: comandos, estado e navegação MVVM.
- `Models`: dados de usuário, conexões, pulseira e emergência.
- `Services`: banco SQL compartilhado, validação, NFC e Bluetooth simulado.
- `Components`: auxiliares reutilizáveis de interface.
- `Assets`: pasta reservada para ícones, imagens e estilos.

## Observações

A integração Bluetooth/NFC está simulada para permitir o fluxo completo do aplicativo sem hardware conectado. Os serviços `BluetoothService` e `NfcService` concentram os pontos de troca para APIs reais do ESP32 e NFC.
