-- Create a table to store task status information
create table tcs_trans_task_status (
    status_id varchar(500) primary key, -- Column for status ID, used as primary key
    status_name varchar(500), -- Column for status name
    is_active bool -- Column to indicate if status is active or not
);

-- Insert initial data into tcs_trans_task_status table
insert into tcs_trans_task_status values(0, 'ending exception', true);
insert into tcs_trans_task_status values(1, 'created', true);
insert into tcs_trans_task_status values(2, 'executing', true);
insert into tcs_trans_task_status values(3, 'sending', true);
insert into tcs_trans_task_status values(4, 'canceling', true);
insert into tcs_trans_task_status values(5, 'canceled', true);
insert into tcs_trans_task_status values(6, 'resending', true);
insert into tcs_trans_task_status values(9, 'completed', true);
insert into tcs_trans_task_status values(10, 'interrupted', true);

-- Create a function to get task data with pagination
CREATE OR REPLACE FUNCTION get_tcs_trans_task(p_page_size INTEGER, p_page_number INTEGER)
RETURNS TABLE (tran_task_num VARCHAR, date_chg TIMESTAMP, date_cr TIMESTAMP, status_name VARCHAR, task_typ VARCHAR, user_call_node VARCHAR, via VARCHAR)
AS $$
DECLARE
    v_offset INTEGER; -- Variable to store pagination offset
BEGIN
    v_offset := (p_page_number - 1) * p_page_size; -- Calculate pagination offset
  
    -- Return query result
    RETURN QUERY
        SELECT t.tran_task_num, t.date_chg, t.date_cr, tttt.status_name, t.task_typ, t.user_call_code, t.via
        FROM tcs_trans_task t
        LEFT JOIN tcs_trans_task_status tttt ON t.task_status = tttt.status_id -- Join with tcs_trans_task_status table
        ORDER BY t.date_cr DESC -- Order by date_cr in descending order
        LIMIT p_page_size -- Limit result to p_page_size rows
        OFFSET v_offset; -- Apply pagination offset
END;
$$
LANGUAGE plpgsql; -- Specify PL/pgSQL as the procedural language for the function
