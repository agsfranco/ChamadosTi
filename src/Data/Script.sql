-- Cria a Role Admin com um GUID novo (caso não exista)
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) 
VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());

SELECT Id, Name FROM AspNetRoles WHERE Name = 'ADMIN';

SELECT Id, UserName FROM AspNetUsers WHERE Email = 'teste@teste.com';

INSERT INTO AspNetUserRoles (UserId, RoleId) 
VALUES ('755f7eef-eeda-41c2-8979-ecc27306c104', '365B110C-F87D-440C-B912-14B395FFC744');

UPDATE AspNetRoles 
SET NormalizedName = 'ADMIN' 
WHERE Name = 'Admin';

-- Verifica se a Role 'Tecnico' já existe. Se não existir, insere o registro normalizado
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Tecnico')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) 
    VALUES (NEWID(), 'Tecnico', 'TECNICO', NEWID());
END

  INSERT INTO SETORES (NOME,DESCRICAO) VALUES ('TI','Infraestrutura de Ti');
  INSERT INTO SETORES (NOME,DESCRICAO) VALUES ('RH','Resursos Humanos');
  INSERT INTO SETORES (NOME,DESCRICAO) VALUES ('Compras','Compras Gerais');
  INSERT INTO SETORES (NOME,DESCRICAO) VALUES ('Almoxarifado','Almoxarifado Geral');

