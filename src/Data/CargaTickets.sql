BEGIN
    SET NOCOUNT ON;

    DECLARE @IdUsuarioComum INT = 4;
    DECLARE @IdTecnicoReal INT = 3;

    -- 1. Limpeza rigorosa da massa de testes anterior
    DELETE FROM [AuditoriasTickets] WHERE [TicketId] IN (SELECT [Id] FROM [Tickets] WHERE [UsuarioId] = @IdUsuarioComum);
    DELETE FROM [ItensTickets] WHERE [TicketId] IN (SELECT [TicketId] FROM [Tickets] WHERE [UsuarioId] = @IdUsuarioComum);
    DELETE FROM [Tickets] WHERE [UsuarioId] = @IdUsuarioComum;

    -- 2. Recupera chaves de catálogo da Fluent API
    DECLARE @StatusAberto INT = (SELECT TOP 1 Id FROM [Status] WHERE [Nome] = 'Status_Aberto');
    DECLARE @StatusAtendimento INT = (SELECT TOP 1 Id FROM [Status] WHERE [Nome] = 'Status_EmAtendimento');
    DECLARE @StatusPendente INT = (SELECT TOP 1 Id FROM [Status] WHERE [Nome] = 'Status_Pendente');
    DECLARE @StatusResolvido INT = (SELECT TOP 1 Id FROM [Status] WHERE [Nome] = 'Status_Resolvido');

    DECLARE @SetorId INT = (SELECT TOP 1 [SetorId] FROM [Usuarios] WHERE [Id] = @IdUsuarioComum);
    DECLARE @ItemInventarioId INT = (SELECT TOP 1 Id FROM [Itens]);

    PRINT 'Iniciando geração de volumetria aleatória orgânica corrigida...';

    DECLARE @DiaRetroativo INT = 0;
    DECLARE @ChamadosNoDia INT;
    DECLARE @ContadorInterno INT;
    
    DECLARE @DataBase DATETIME2;
    DECLARE @DataAbertura DATETIME2;
    DECLARE @PrazoSlaRESOL DATETIME2;
    DECLARE @DataFechamento DATETIME2;
    
    DECLARE @StatusId INT;
    DECLARE @PrioridadeId INT;
    DECLARE @CategoriaId INT;
    DECLARE @Titulo NVARCHAR(100);
    DECLARE @TicketId INT;
    DECLARE @LogTexto NVARCHAR(MAX);

    -- Loop que viaja pelos últimos 30 dias
    WHILE @DiaRetroativo < 30
    BEGIN
        SET @DataBase = DATEADD(day, -@DiaRetroativo, GETUTCDATE());
        
        -- Sorteia quantos chamados aconteceram neste dia específico (entre 1 e 5 chamados para evitar dias zerados)
        SET @ChamadosNoDia = FLOOR(RAND(CHECKSUM(NEWID())) * 5) + 1;
        SET @ContadorInterno = 0;

        WHILE @ContadorInterno < @ChamadosNoDia
        BEGIN
            -- CORREÇÃO: Converte explicitamente para DATETIME2 antes de injetar os minutos randômicos
            SET @DataAbertura = DATEADD(minute, FLOOR(RAND(CHECKSUM(NEWID())) * 1440), CAST(CAST(@DataBase AS DATE) AS DATETIME2));
            
            -- Sorteia dinamicamente a Prioridade e Categoria existentes no catálogo
            SET @PrioridadeId = (SELECT TOP 1 Id FROM [Prioridades] ORDER BY NEWID());
            SET @CategoriaId = (SELECT TOP 1 Id FROM [Categorias] ORDER BY NEWID());

            -- Sorteia o cenário de atendimento e desfecho de SLA (Distribuição Randômica)
            DECLARE @SorteioCenario INT = FLOOR(RAND(CHECKSUM(NEWID())) * 10);
            
            IF @SorteioCenario < 4 -- 40% Chance: Resolvido NO PRAZO
            BEGIN
                SET @StatusId = @StatusResolvido;
                SET @PrazoSlaRESOL = DATEADD(hour, 8, @DataAbertura);
                SET @DataFechamento = DATEADD(hour, FLOOR(RAND(CHECKSUM(NEWID())) * 5) + 1, @DataAbertura);
                SET @Titulo = 'Incidente resolvido operacional - Ref #' + CAST(ABS(CHECKSUM(NEWID())) % 1000 AS VARCHAR(4));
            END
            ELSE IF @SorteioCenario < 6 -- 20% Chance: Resolvido FORA DO PRAZO (Vencido)
            BEGIN
                SET @StatusId = @StatusResolvido;
                SET @PrazoSlaRESOL = DATEADD(hour, 4, @DataAbertura);
                SET @DataFechamento = DATEADD(hour, FLOOR(RAND(CHECKSUM(NEWID())) * 8) + 5, @DataAbertura);
                SET @Titulo = 'Ajuste complexo finalizado - Ref #' + CAST(ABS(CHECKSUM(NEWID())) % 1000 AS VARCHAR(4));
            END
            ELSE IF @SorteioCenario < 8 -- 20% Chance: Backlog Ativo Em Atendimento
            BEGIN
                SET @StatusId = @StatusAtendimento;
                SET @PrazoSlaRESOL = DATEADD(hour, 8, @DataAbertura);
                SET @DataFechamento = NULL;
                SET @Titulo = 'Análise técnica em execução - Ref #' + CAST(ABS(CHECKSUM(NEWID())) % 1000 AS VARCHAR(4));
            END
            ELSE -- 20% Chance: Backlog Ativo Aberto na triagem
            BEGIN
                SET @StatusId = @StatusAberto;
                SET @PrazoSlaRESOL = DATEADD(hour, 12, @DataAbertura);
                SET @DataFechamento = NULL;
                SET @Titulo = 'Nova requisição aguardando analista - Ref #' + CAST(ABS(CHECKSUM(NEWID())) % 1000 AS VARCHAR(4));
            END

            -- Inserção na tabela Tickets aproveitando a coluna anulável
            INSERT INTO [Tickets] 
                ([UsuarioId], [SetorId], [CategoriaId], [PrioridadeId], [StatusId], [DataAbertura], [PrazoRespostaSLA], [PrazoResolucaoSLA], [DataFechamento], [Titulo], [Comentario], [Ativo], [UsuarioTecnicoId], [TicketOrigemId])
            VALUES 
                (@IdUsuarioComum, @SetorId, @CategoriaId, @PrioridadeId, @StatusId, @DataAbertura, DATEADD(hour, 2, @DataAbertura), @PrazoSlaRESOL, @DataFechamento, @Titulo, 'Massa de testes randômica estruturada para validação de curvas de tendência e fatiamento proporcional do dashboard de SLA.', 1, CASE WHEN @StatusId <> @StatusAberto THEN @IdTecnicoReal ELSE NULL END, NULL);

            SET @TicketId = SCOPE_IDENTITY();

            IF @TicketId IS NOT NULL AND @ItemInventarioId IS NOT NULL
            BEGIN
                INSERT INTO [ItensTickets] ([ItemId], [TicketId], [Comentario]) VALUES (@ItemInventarioId, @TicketId, 'Ativo mapeado na amostragem.');
            END

            IF @TicketId IS NOT NULL
            BEGIN
                SET @LogTexto = CASE WHEN @DataFechamento IS NOT NULL 
                    THEN '{"Acao":"Log_ResolucaoChamado","Usuario":"Service Desk","CamposModificados":{"Status_Nome":"Status_Resolvido"},"Mensagem":"Atendimento concluído com encerramento de SLA."}'
                    ELSE '{"Acao":"Log_StatusInicial","Usuario":"Sistema","CamposModificados":{"Status_Nome":"Status_Aberto"},"Mensagem":"Ticket inserido na malha de volumetria."}' END;

                INSERT INTO [AuditoriasTickets] ([TicketId], [Log], [RegistradoEm]) 
                VALUES (@TicketId, @LogTexto, COALESCE(@DataFechamento, @DataAbertura));
            END

            SET @ContadorInterno = @ContadorInterno + 1;
        END;

        SET @DiaRetroativo = @DiaRetroativo + 1;
    END;

    PRINT '----------------------------------------------------------------------';
    PRINT ' SUCESSO TOTAL: Amostragem estocástica com CAST de tempo homologada!';
    PRINT '----------------------------------------------------------------------';
END;
