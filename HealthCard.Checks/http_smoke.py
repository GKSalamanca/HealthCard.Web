"""End-to-end checks against the isolated Testing host on localhost:5168 only."""
from pathlib import Path
from html.parser import HTMLParser
import base64, csv, datetime, html, http.cookiejar, io, json, re, sys, urllib.request, urllib.parse, urllib.error, uuid

BASE='http://127.0.0.1:5168'
OUT=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).parent/'artifacts'
OUT.mkdir(parents=True,exist_ok=True)
checks=[]
def check(name, condition):
    if not condition: raise AssertionError(name)
    checks.append(name); print('PASS',name,flush=True)

class Dom(HTMLParser):
    def __init__(self,text):
        super().__init__(); self.inputs=[]; self.links=[]; self.canvases=[];self.feed(text)
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if tag=='input': self.inputs.append(a)
        if tag=='a' and 'href' in a:self.links.append(a['href'])
        if tag=='canvas':self.canvases.append(a)
    def hidden(self): return {a['name']:a.get('value','') for a in self.inputs if a.get('type')=='hidden' and a.get('name')}
    def values(self,name):return [a.get('value','') for a in self.inputs if a.get('name')==name]

class Client:
    def __init__(self):
        self.cookies=http.cookiejar.CookieJar();self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.cookies))
    def request(self,path,data=None,headers=None):
        request=urllib.request.Request(path if path.startswith(BASE) else BASE+path,data=data,headers=headers or {})
        try:r=self.opener.open(request,timeout=45)
        except urllib.error.HTTPError as error:r=error
        body=r.read();raw=body.decode('utf-8',errors='replace');return r.code,html.unescape(raw),r.geturl(),body,dict(r.headers),raw
    def get(self,path):return self.request(path)
    def post(self,path,fields,form=None,attachment=None,csrf=True):
        page=self.get(form or path)
        defaults=Dom(page[5]).hidden() if csrf else {}
        fields={**defaults,**fields}
        if not csrf: fields.pop('__RequestVerificationToken',None)
        if attachment:
            boundary='----HealthCard'+uuid.uuid4().hex;parts=[]
            for name,value in fields.items(): parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode())
            filename,content=attachment;parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="File"; filename="{filename}"\r\nContent-Type: application/octet-stream\r\n\r\n'.encode()+content+b'\r\n');parts.append(f'--{boundary}--\r\n'.encode())
            return self.request(path,b''.join(parts),{'Content-Type':'multipart/form-data; boundary='+boundary})
        return self.request(path,urllib.parse.urlencode(fields).encode(),{'Content-Type':'application/x-www-form-urlencoded'})

anonymous=Client(); initial=anonymous.get('/Account/Login')
check('Host is explicitly Testing',initial[0]==200 and initial[4].get('X-HealthCard-Environment')=='Testing')
check('Anonymous medical records require login','/Account/Login' in anonymous.get('/MedicalRecord')[2])
stamp=uuid.uuid4().hex[:8]; password='HealthCard1!'; new_password='HealthCard2!'
def account(suffix):
    c=Client();email=f'qa.{suffix}.{stamp}@example.test'
    r=c.post('/Account/Register',{'FullName':'QA '+suffix,'Email':email,'Password':password,'ConfirmPassword':password})
    check('Register '+suffix,r[0]==200 and '/Account/Login' in r[2])
    r=c.post('/Account/Login',{'Email':email,'Password':password})
    check('Login '+suffix,r[0]==200 and '/Account/Login' not in r[2])
    return c,email

a,email_a=account('A')
for path in ['/','/MedicalRecord','/Category','/Category/GroupDetails?type=LabTest','/Category/GroupDetails?type=Research','/Analytics','/Norms','/Account/Profile','/Account/ChangePassword','/Home/Error']:
    r=a.get(path);check('Page '+path,r[0]==200 and '/Account/Login' not in r[2])
for asset in ['/lib/bootstrap/dist/css/bootstrap.min.css','/lib/jquery/dist/jquery.min.js','/lib/bootstrap-icons/bootstrap-icons.min.css','/lib/bootstrap-icons/fonts/bootstrap-icons.woff2','/js/site.js']:
    check('Local asset '+asset,a.get(asset)[0]==200)

name='QA Витамины '+stamp
category_fields={'Id':'0','Name':name,'Description':'Проверка собственных показателей','IsLabTest':'true',
    'Parameters[0].Name':'Витамин D','Parameters[0].Unit':'нг/мл','Parameters[0].NormMin':'30','Parameters[0].NormMax':'100',
    'Parameters[1].Name':'Цинк','Parameters[1].Unit':'мкг/дл','Parameters[1].NormMin':'50','Parameters[1].NormMax':'120'}
r=a.post('/Category/Create',category_fields)
(OUT/'category-create.html').write_text(r[1],encoding='utf-8')
check('Create own laboratory category',r[0]==200 and '/CategoryRecords/Details/' in r[2])
category=int(r[2].rstrip('/').split('/')[-1])
check('Own category listed',name in a.get('/Category')[1])
create_url=f'/MedicalRecord/Create?categoryId={category}'
form=Dom(a.get(create_url)[5]); ids=[int(x['value']) for x in form.inputs if x.get('name','').endswith('.TestParameterId')]
check('Custom parameters loaded',len(ids)==2)
partial=a.get(f'/MedicalRecord/Parameters?categoryId={category}')
check('AJAX parameter partial',partial[0]==200 and 'Витамин D' in partial[1] and 'Цинк' in partial[1])
fields={'Id':'0','CategoryId':str(category),'RecordDate':'2026-11-01','Title':'QA анализ первый','Parameters[0].TestParameterId':str(ids[0]),'Parameters[0].Value':'1,025','Parameters[1].TestParameterId':str(ids[1]),'Parameters[1].Value':'70'}
for label,override in [('Missing title',{'Title':''}),('Future date',{'RecordDate':'2099-01-01'}),('Negative result',{'Parameters[0].Value':'-1'}),('Foreign parameter',{'Parameters[0].TestParameterId':'1'}),('Repeated parameter',{'Parameters[1].TestParameterId':str(ids[0])})]:
    r=a.post('/MedicalRecord/Create',{**fields,**override},form=create_url)
    check(label+' rejected',r[0]==200 and '/MedicalRecord/Details/' not in r[2])
png=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jA1sAAAAASUVORK5CYII=')
r=a.post('/MedicalRecord/Create',fields,form=create_url,attachment=('test.png',png))
(OUT/'record-create.html').write_text(r[1],encoding='utf-8')
check('Create record with attachment',r[0]==200 and '/MedicalRecord/Details/' in r[2])
record=int(r[2].rstrip('/').split('/')[-1]);records=[record]
check('Results visible in details','Витамин D' in r[1] and 'Цинк' in r[1] and ('1.025' in r[1] or '1,025' in r[1]))
download=a.get(f'/MedicalRecord/Download/{record}')
check('Owner downloads unchanged attachment',download[0]==200 and download[3]==png)
check('Anonymous download requires login','/Account/Login' in anonymous.get(f'/MedicalRecord/Download/{record}')[2])
check('Static uploads are blocked',anonymous.get('/uploads/any/test.png')[0]==404)
for date,value in [('2026-11-01','35'),('2026-11-10','45')]:
    r=a.post('/MedicalRecord/Create',{**fields,'RecordDate':date,'Title':'QA анализ '+date+' '+value,'Parameters[0].Value':value},form=create_url)
    check('Create next result '+value,r[0]==200 and '/MedicalRecord/Details/' in r[2]); records.append(int(r[2].rstrip('/').split('/')[-1]))
matrix=a.get(f'/CategoryRecords/Details/{category}')
check('Same-day records have separate columns',all(f'Запись №{x}' in matrix[1] for x in records))
analytics_url=f'/Analytics?categoryId={category}&parameterId={ids[0]}'
r=a.get(analytics_url);(OUT/'analytics.html').write_text(r[5],encoding='utf-8');chart=Dom(r[5]).canvases
check('Analytics chart rendered',r[0]==200 and len(chart)==1)
series=json.loads(chart[0]['data-series'])
check('Analytics series contains every measurement',len(series)==3 and series[0]['value']==1.025 and [x['recordId'] for x in series]==records)
export=f'/Analytics/Export?categoryId={category}&parameterId={ids[0]}'
csv_response=a.get(export+'&format=csv');csv_rows=list(csv.reader(io.StringIO(csv_response[3].decode('utf-8-sig')),delimiter=';'))
check('CSV exports all measurements',csv_response[0]==200 and len(csv_rows)==4 and csv_rows[1][3]=='1.0250')
pdf=a.get(export+'&format=pdf');(OUT/'analysis.pdf').write_bytes(pdf[3]);check('PDF exports real document',pdf[0]==200 and pdf[3].startswith(b'%PDF-') and len(pdf[3])>1000)
filtered=a.get(export+'&format=csv&fromDate=2026-11-02')
check('Date filtering reaches export',len(list(csv.reader(io.StringIO(filtered[3].decode('utf-8-sig')),delimiter=';')))==2)
check('Inverted date filter rejected','Начальная дата' in a.get(analytics_url+'&fromDate=2026-12-01&toDate=2026-01-01')[1])
norm=a.post(f'/Norms/Edit/{ids[0]}',{'TestParameterId':str(ids[0]),'NormMin':'0','NormMax':'10'})
check('Personal norm saved',norm[0]==200 and '/Norms?' in norm[2])
normcsv=list(csv.reader(io.StringIO(a.get(export+'&format=csv')[3].decode('utf-8-sig')),delimiter=';'))
check('Personal norm used by analytics',normcsv[1][5]=='0.0000' and normcsv[1][6]=='10.0000' and normcsv[1][7]=='В интервале')
check('Personal norm used by details','(мой)' in a.get(f'/MedicalRecord/Details/{record}')[1])
check('Personal norm used by matrix','table-success' in a.get(f'/CategoryRecords/Details/{category}')[1])
norm_bad=a.post(f'/Norms/Edit/{ids[0]}',{'TestParameterId':str(ids[0]),'NormMin':'20','NormMax':'10'})
check('Invalid personal interval rejected','Нижняя граница' in norm_bad[1])
a.post(f'/Norms/Reset/{ids[0]}',{},form='/Norms')
resetcsv=list(csv.reader(io.StringIO(a.get(export+'&format=csv')[3].decode('utf-8-sig')),delimiter=';'))
check('Reset restores default norm',resetcsv[1][5]=='30.0000' and resetcsv[1][7]=='Вне интервала')

edit_fields={**fields,'Id':str(record),'Title':'QA запись отредактирована','Parameters[0].Value':'2.025'}
r=a.post(f'/MedicalRecord/Edit/{record}',edit_fields)
check('Record editing saves values',r[0]==200 and '/Details/' in r[2] and 'QA запись отредактирована' in r[1] and ('2.025' in r[1] or '2,025' in r[1]))
check('Editing keeps attachment',a.get(f'/MedicalRecord/Download/{record}')[3]==png)
r=a.post(f'/MedicalRecord/Edit/{record}',{**edit_fields,'RemoveFile':'true'})
check('Attachment removal works',r[0]==200 and a.get(f'/MedicalRecord/Download/{record}')[0]==404)
bad_file=a.post('/MedicalRecord/Create',fields,form=create_url,attachment=('fake.pdf',b'not a PDF'))
check('Fake file rejected','Загрузите настоящий' in bad_file[1] and '/Details/' not in bad_file[2])

edit_category={**category_fields,'Id':str(category),'Name':name+' изменено','Parameters[0].Id':str(ids[0]),'Parameters[1].Id':str(ids[1]),'Parameters[2].Name':'Магний','Parameters[2].Unit':'ммоль/л','Parameters[2].NormMin':'0.7','Parameters[2].NormMax':'1.1'}
r=a.post(f'/Category/Edit/{category}',edit_category)
check('Category editing adds parameter',r[0]==200 and '/CategoryRecords/Details/' in r[2] and 'Магний' in r[1])
r=a.post(f'/Category/Edit/{category}',{**edit_category,'Parameters[0].Remove':'true'})
check('Used parameter cannot be removed','Нельзя удалить показатель' in r[1] and '/Category/Edit/' in r[2])
r=a.post(f'/Category/Delete/{category}',{})
check('Category with history cannot be removed',r[0]==200 and a.get(f'/CategoryRecords/Details/{category}')[0]==200)
check('System category cannot be edited',a.get('/Category/Edit/1')[0]==404)
check('System category cannot be deleted',a.get('/Category/Delete/1')[0]==404)
check('CSRF token required',a.post(f'/Category/Delete/{category}',{},csrf=False)[0]==400)

doc_name='QA Документы '+stamp
r=a.post('/Category/Create',{'Id':'0','Name':doc_name,'IsLabTest':'false','Parameters[0].Name':'','Parameters[0].Unit':''})
check('Own document category without parameters',r[0]==200 and '/CategoryRecords/Details/' in r[2]);doc_id=int(r[2].rstrip('/').split('/')[-1])
doc_url=f'/MedicalRecord/Create?categoryId={doc_id}'
r=a.post('/MedicalRecord/Create',{'Id':'0','CategoryId':str(doc_id),'RecordDate':'2026-11-11','Title':'QA заключение','Comment':'Проверка категории документов'},form=doc_url)
check('Document-only record created',r[0]==200 and '/MedicalRecord/Details/' in r[2]);doc_record=int(r[2].rstrip('/').split('/')[-1])

profile=a.post('/Account/Profile',{'FullName':'QA Обновленный профиль','Email':email_a,'DateOfBirth':'1995-06-15','Gender':'1','BloodType':'A(II)+'})
check('Profile can be edited',profile[0]==200 and 'QA Обновленный профиль' in a.get('/')[1])
bad_password=a.post('/Account/ChangePassword',{'CurrentPassword':'wrong','NewPassword':new_password,'ConfirmPassword':new_password})
check('Wrong current password rejected','Текущий пароль неверен' in bad_password[1])
changed=a.post('/Account/ChangePassword',{'CurrentPassword':password,'NewPassword':new_password,'ConfirmPassword':new_password})
check('Password can be changed',changed[0]==200 and '/Account/Profile' in changed[2])
a.post('/Account/Logout',{},form='/Account/Profile')
check('Logout works','/Account/Login' in a.get('/')[2])
check('Old password no longer works','/Account/Login' in a.post('/Account/Login',{'Email':email_a,'Password':password})[2])
check('New password works','/Account/Login' not in a.post('/Account/Login',{'Email':email_a,'Password':new_password})[2])

b,email_b=account('B')
check('Other user does not see private category',name not in b.get('/Category')[1])
for path in [f'/CategoryRecords/Details/{category}',f'/Category/Edit/{category}',f'/Category/Delete/{category}',f'/MedicalRecord/Details/{record}',f'/MedicalRecord/Edit/{record}',f'/MedicalRecord/Delete/{record}',f'/MedicalRecord/Parameters?categoryId={category}',f'/Norms/Edit/{ids[0]}']:
    check('Owner isolation '+path,b.get(path)[0]==404)
check('Private parameter names do not leak through analytics','Витамин D' not in b.get(analytics_url)[1])
check('Private export denied',b.get(export+'&format=csv')[0]==400)
check('Other user cannot delete record',b.post(f'/MedicalRecord/Delete/{record}',{},form='/Account/Profile')[0]==404)
check('Record retained after unauthorized request',a.get(f'/MedicalRecord/Details/{record}')[0]==200)

# Exercise paging, export escaping and upload boundaries against disposable data.
r=a.post('/Category/Create',{**category_fields,'Name':'Липидный профиль'})
check('Duplicate base category rejected','Категория с таким названием' in r[1] and '/Category/Create' in r[2])
r=a.post('/Category/Create',{**category_fields,'Parameters[1].Name':'витамин d'})
check('Duplicate parameter names rejected','Названия показателей не должны повторяться' in r[1])
for filename,content,message in [('empty.pdf',b'','Выбранный файл пуст'),('large.png',png+b'0'*(10*1024*1024),'Размер файла не должен превышать')]:
    r=a.post('/MedicalRecord/Create',fields,form=create_url,attachment=(filename,content))
    check(filename+' rejected',r[0]==200 and message in r[1] and '/Details/' not in r[2])
r=a.post(f'/MedicalRecord/Edit/{record}',edit_fields,attachment=('replacement.png',png))
check('Attachment replacement saved',r[0]==200 and a.get(f'/MedicalRecord/Download/{record}')[3]==png)
check('Other account cannot download existing attachment',b.get(f'/MedicalRecord/Download/{record}')[0]==404)
check('Download is private and not sniffed',a.get(f'/MedicalRecord/Download/{record}')[4].get('X-Content-Type-Options')=='nosniff' and 'no-store' in a.get(f'/MedicalRecord/Download/{record}')[4].get('Cache-Control',''))

# A physical legacy file must stay inaccessible through static middleware.
legacy_root=Path(__file__).resolve().parent.parent/'HealthCard.Web'/'wwwroot'/'uploads'/('qa-disposable-'+stamp)
legacy_file=legacy_root/'test.png'
try:
    legacy_root.mkdir(parents=True,exist_ok=False);legacy_file.write_bytes(png)
    for prefix in ['uploads','UPLOADS']:
        check('Physical legacy file blocked '+prefix,anonymous.get(f'/{prefix}/{legacy_root.name}/test.png')[0]==404)
finally:
    if legacy_file.exists():legacy_file.unlink()
    if legacy_root.exists():legacy_root.rmdir()

empty=a.get(analytics_url+'&fromDate=2099-01-01')
check('Empty analytics has no broken chart',empty[0]==200 and not Dom(empty[5]).canvases and 'Нет измерений' in empty[1])
check('Unsupported export format rejected',a.get(export+'&format=exe')[0]==400)
check('Wrong parameter filter rejected',a.get(f'/Analytics/Export?categoryId={category}&parameterId=1')[0]==400)
invalid_profile=a.post('/Account/Profile',{'FullName':'QA','Gender':'999'})
check('Invalid profile enum rejected',invalid_profile[0]==200 and 'validation-summary-errors' in invalid_profile[1] and 'QA Обновленный профиль' in a.get('/')[1])
check('Medical record POST requires CSRF',a.post('/MedicalRecord/Create',fields,csrf=False)[0]==400)
check('Personal norm POST requires CSRF',a.post(f'/Norms/Edit/{ids[0]}',{'NormMin':'1','NormMax':'2'},csrf=False)[0]==400)
r=a.post(f'/Category/Edit/{category}',{**edit_category,'Name':'=QA formula '+stamp})
formula_csv=list(csv.reader(io.StringIO(a.get(export+'&format=csv')[3].decode('utf-8-sig')),delimiter=';'))
check('CSV neutralizes spreadsheet formulas',r[0]==200 and formula_csv[1][1].startswith("'=QA"))
r=a.post(f'/Category/Edit/{category}',{**edit_category,'Parameters[2].Remove':'true'})
check('Unused category parameter can be removed',r[0]==200 and 'Магний' not in a.get(create_url)[1])
for n in range(10):
    r=a.post('/MedicalRecord/Create',{**fields,'Title':'QA страница '+str(n),'RecordDate':'2026-10-01'},form=create_url)
    assert '/MedicalRecord/Details/' in r[2],r[1]
    records.append(int(r[2].rstrip('/').split('/')[-1]))
def listed_ids(response):
    return [int(link.rsplit('/',1)[-1]) for link in Dom(response[5]).links if re.fullmatch(r'/MedicalRecord/Details/\d+',link)]
page1=a.get(f'/MedicalRecord?categoryId={category}')
page2=a.get(f'/MedicalRecord?categoryId={category}&page=2')
check('Record pages have 12 and 1 items',len(listed_ids(page1))==12 and len(listed_ids(page2))==1)
check('Pagination does not repeat records',set(listed_ids(page1)).isdisjoint(listed_ids(page2)) and set(listed_ids(page1)+listed_ids(page2))==set(records))
check('Pagination keeps category filter',any('categoryId='+str(category) in link and 'page=2' in link for link in Dom(page1[5]).links))
check('Last page clamped safely',listed_ids(a.get(f'/MedicalRecord?categoryId={category}&page=999'))==listed_ids(page2))
check('First page clamped safely',listed_ids(a.get(f'/MedicalRecord?categoryId={category}&page=0'))==listed_ids(page1))
check('Record list date filter',len(listed_ids(a.get(f'/MedicalRecord?categoryId={category}&fromDate=2026-11-02')))==1)

for rid in [doc_record]+records:
    r=a.post(f'/MedicalRecord/Delete/{rid}',{})
    check('Delete owned record '+str(rid),r[0]==200 and a.get(f'/MedicalRecord/Details/{rid}')[0]==404)
for cid in [doc_id,category]:
    a.post(f'/Category/Delete/{cid}',{})
    check('Delete empty own category '+str(cid),a.get(f'/CategoryRecords/Details/{cid}')[0]==404)

(OUT/'http-results.json').write_text(json.dumps({'verified_at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'environment':'Testing','host':BASE,'passed':len(checks),'checks':checks,'fake_accounts':[email_a,email_b]},ensure_ascii=False,indent=2),encoding='utf-8')
print('HTTP checks passed:',len(checks),flush=True)
