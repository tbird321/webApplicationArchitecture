### Site Configuration
/public/config.json

### AWS Component deployment
aws codeartifact login --tool npm --repository reactComponents --domain tbirdcomponents --domain-owner 646797148861 --region us-west-2

### update components to latest version
update package.json  for @tbirdcomponents/reactcomponents line to be new version
npm install

### Deploy to AWS

### For Production Builds
set GENERATE_SOURCEMAP=false
set NODE_ENV=production

### Then run the Build
npm run build

### Then Sync the build folder to the S3 bucket
aws s3 sync build/ s3://www.ldsfaithincrisis.com --exclude "appconfig.json" --exclude "sitemenu.json"


## set a users password via aws cli

aws cognito-idp admin-set-user-password --user-pool-id us-west-2_KRm2nfNtS --username {username} --password {password} --permanent
