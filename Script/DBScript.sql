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

-- Create a function --
CREATE OR REPLACE FUNCTION count_tcs_task_by_status(
    p_task_status varchar, 
    p_task_typ varchar, 
    p_wb_codes varchar[]
)
RETURNS TABLE ("Quantity" int) AS
$$
DECLARE
    count_result int; -- Declare a variable to hold the result of the count query
BEGIN
    -- Execute a count query to count the rows that match the input criteria
    SELECT count(*) INTO count_result
    FROM tcs_trans_task
    WHERE task_status = p_task_status
        AND task_typ = p_task_typ
        AND wb_code = ANY(p_wb_codes);
    
    -- Return the count result as a single-row table
    RETURN QUERY SELECT count_result;
END;
$$
LANGUAGE plpgsql;

drop function get_tcs_pod

-- Create a function --
CREATE OR REPLACE FUNCTION public.get_tcs_pod(p_position character varying, p_task_status varchar)
 RETURNS TABLE(podcode character varying, casenum character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY SELECT tp.pod_code, tp.case_num 
    FROM tcs_map_data tmd
    left join tcs_pod tp 
    on tmd.pod_code = tp.pod_code 
    WHERE tp.src_berth_code  = p_position and tp.case_num IS not NULL OR tp.case_num<> '' and ttt.task_status = p_task_status
    limit 1;
END;
$function$
;

-- Create a function --
CREATE OR REPLACE FUNCTION public.find_pod_by_position(p_position character varying)
 RETURNS TABLE(place character varying, podcode character varying)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY SELECT tmd.map_data_code, tmd.pod_code
    FROM tcs_map_data tmd
    where tmd.map_data_code = p_position
    limit 1;
END;
$function$
;

CREATE OR REPLACE FUNCTION public.find_free_pod_by_area(p_area character varying)
RETURNS TABLE(podcode character varying)
LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY SELECT tmd.pod_code
    FROM tcs_map_data tmd
    LEFT JOIN tcs_pod tp 
        ON tp.pod_code = tmd.pod_code 
    WHERE tmd.area_code = p_area AND (tp.case_num IS NULL OR tp.case_num = '')
    ORDER BY coo_x asc
   	limit 1;
END;
$function$
;

drop function find_free_pod_by_area
