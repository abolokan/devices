# Devices.Transport.Abstractions

- Назначение: абстракции транспорта и адресации.
- Ключевые типы: `ITransport`, `ITransportFactory`, `EndpointAddress`.
- Используется: ядром, плагинами, транспортными реализациями.

Позволяет писать код, независимый от носителя (TCP/Serial/USB).
