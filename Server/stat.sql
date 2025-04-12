SELECT
date(timestamp) as log_date,
max(cur_user_num) as max_user,
count(CASE WHEN event='visit' THEN 1 END) as visit,
count(CASE WHEN event='login' THEN 1 END) as login,
count(CASE WHEN event='start_pve' THEN 1 END) as start_pve,
count(CASE WHEN event='start_pvp' THEN 1 END) as start_pvp,
count(CASE WHEN event='start_match' THEN 1 END) as start_match

FROM stat

GROUP by log_date

