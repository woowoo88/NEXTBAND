# NextBand

Aplicativo desktop desenvolvido em C# WPF para configuração de uma pulseira inteligente com NFC, Bluetooth, ESP32, LED RGB e display OLED.

## Funcionalidades

- Login e cadastro de usuário
- Dashboard com perfil e status da pulseira
- Compartilhamento de perfil via NFC
- Registro de conexões recentes
- Tela de conexões com busca
- Edicao de perfil
- Configuracao da pulseira NextBand
- Conexao Bluetooth simulada com ESP32
- Controle de LED RGB
- Configuracao do texto do display OLED
- Link rapido para Instagram, LinkedIn ou URL
- URL personalizada de emergência
- Página pública de emergência infantil
- Modo criança
- Controle de permissoes
- Armazenamento local em JSON

## Tecnologias utilizadas

- C#
- WPF
- XAML
- MVVM
- JSON para armazenamento local
- Servicos preparados para Bluetooth, NFC e ESP32

## Como executar

1. Clonar o repositorio:

```bash
git clone https://github.com/woowoo88/NEXTBAND.git
```

2. Abrir o projeto no Visual Studio.

3. Restaurar dependencias.

4. Compilar e executar o projeto.

Ou pelo terminal:

```bash
dotnet build
dotnet run
```

## Estrutura do projeto

- `Views`: telas XAML do aplicativo.
- `ViewModels`: comandos, estado e navegacao MVVM.
- `Models`: dados de usuário, conexões, pulseira e emergência.
- `Services`: armazenamento local, validacao, NFC e Bluetooth simulado.
- `Components`: auxiliares reutilizaveis de interface.
- `Assets`: pasta reservada para icones, imagens e estilos.

## Observacoes

A integracao Bluetooth/NFC esta simulada para permitir o fluxo completo do aplicativo sem hardware conectado. Os servicos `BluetoothService` e `NfcService` concentram os pontos de troca para APIs reais do ESP32 e NFC.
