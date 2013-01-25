-- Add your test scenario here --

select sys.tables.name, sys.columns.name, sys.columns.max_length / 2 from sys.tables
inner join sys.columns on
	sys.columns.object_id = sys.tables.object_id and
	sys.tables.name not like 'sys%' and
	sys.tables.name not like 'dt%' and
	sys.columns.system_type_id = 231
order by sys.tables.name
	
select sys.procedures.name, sys.parameters.name, sys.parameters.max_length / 2 from sys.parameters
inner join sys.procedures on
	sys.procedures.object_id = sys.parameters.object_id and
	sys.parameters.system_type_id = 231 and
	sys.parameters.name != '@OrderBy' and
	sys.procedures.name != 'PageSelect' and
	sys.procedures.name not like 'Blog%' and
	sys.procedures.name not like 'dt_%' and
	sys.procedures.name not like 'sp_%'
order by sys.procedures.name