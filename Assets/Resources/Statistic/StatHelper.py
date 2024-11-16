
def add_id(file_path):
    lines = []
    with open(file_path, 'r', encoding='utf-8') as file:
        lines = file.readlines()

    new_lines = []
    meet_cards_tag = False
    id = 2001 # 第一位：久二年。第二位：0角色牌，1其他。最后两位：递增id
    for line in lines:
        new_lines.append(line)
        if "\"Cards\"" in line:
            meet_cards_tag = True
        if not meet_cards_tag:
            continue
        if '{' in line:
            new_lines.append("            \"infoId\": %d,\n" % (id))
            id += 1

    # 将修改后的内容写入新文件
    with open(file_path, 'w', encoding='utf-8') as file:
        file.writelines(new_lines)

add_id("KumikoSecondYear.json")
