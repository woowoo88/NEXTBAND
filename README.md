# NextBand

Aplicativo desktop desenvolvido em C# WPF para configuração de uma pulseira inteligente com NFC, Bluetooth e ESP32.

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
- Armazenamento local em JSON

## Tecnologias utilizadas

- C#
- WPF
- XAML
- MVVM
- JSON com `System.Text.Json`
- Serviços preparados para Bluetooth, NFC e ESP32

## Armazenamento

O app salva os dados localmente em JSON no caminho:

```text
%AppData%\NextBand\nextband-data.json
```

O login usa o e-mail e a senha salvos nesse arquivo.

## Como executar

1. Abrir o projeto no Visual Studio.
2. Restaurar dependências.
3. Compilar e executar.

Ou pelo terminal:

```bash
dotnet build
dotnet run
```

## Estrutura do projeto

- `Views`: telas XAML do aplicativo.
- `ViewModels`: comandos, estado e navegação MVVM.
- `Models`: dados de usuário, conexões, pulseira e emergência.
- `Services`: armazenamento JSON, validação, NFC e Bluetooth simulado.
- `Components`: auxiliares reutilizáveis de interface.
- `Assets`: pasta reservada para ícones, imagens e estilos.

## Observações

A integração Bluetooth/NFC está simulada para permitir o fluxo completo do aplicativo sem hardware conectado. Os serviços `BluetoothService` e `NfcService` concentram os pontos de troca para APIs reais do ESP32 e NFC.
