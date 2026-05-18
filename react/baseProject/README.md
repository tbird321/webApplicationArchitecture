### Site Configuration
/public/config.json

### AWS Component deployment
The CodeArtifact token expires after 12 hours. Re-run this before npm install if you get auth errors:
aws codeartifact login --tool npm --repository reactComponents --domain tbirdcomponents --domain-owner 646797148861 --region us-west-2

### update components to latest version
update package.json  for @tbirdcomponents/reactcomponents line to be new version
npm install

### Deploy to AWS

### For Production Builds
CMD:
set GENERATE_SOURCEMAP=false
set NODE_ENV=production

PowerShell:
$env:GENERATE_SOURCEMAP="false"
$env:NODE_ENV="production"

### copy the proper configuration to the location

### LDS Apologetics
cp configs/ldsapologetics-Prod.json public/config.json
cp configs/ldsapologetics-initialRender.html public/initialRender.html
cp configs/ldsapologetics-BaseStyles.css public/BaseStyles.css
cp configs/ldsapologetics-favicon.ico public/favicon.ico
cp configs/ldsapologetics-headerImage.jpg public/headerImage.jpg

### LDS Doctrines
cp configs/ldsdoctrines-Prod.json public/config.json
cp configs/ldsdoctrines-initialRender.html public/initialRender.html
cp configs/ldsdoctrines-BaseStyles.css public/BaseStyles.css
cp configs/ldsdoctrines-favicon.ico public/favicon.ico
cp configs/ldsdoctrines-headerImage.jpg public/headerImage.jpg

### Faith In Crisis
cp configs/faithInCrisis-Prod.json public/config.json
cp configs/faithInCrisis-initialRender.html public/initialRender.html
cp configs/faithInCrisis-BaseStyles.css public/BaseStyles.css
cp configs/faithInCrisis-favicon.ico public/favicon.ico
cp configs/faithInCrisis-headerImage.jpg public/headerImage.jpg


### Then run the Build
npm run build

### Then Sync the build folder to the S3 bucket

### LDS Apologetics
aws s3 sync build/ s3://www.ldsapologetics.com --exclude "sitemenu.json"

### LDS Doctrines
aws s3 sync build/ s3://www.ldsdoctrines.com --exclude "sitemenu.json"

### Faith In Crisis
aws s3 sync build/ s3://www.ldsfaithincrisis.com --exclude "sitemenu.json"


## User Management (AWS Cognito)
signUpEnabled is set to false in all site configs — users cannot self-register.
All users must be created by an admin via the AWS CLI.

### Create a new user
aws cognito-idp admin-create-user --user-pool-id us-west-2_KRm2nfNtS --username {username} --user-attributes Name=email,Value={email} --temporary-password {tempPassword}
The user will be forced to change their password on first login.

### Set a permanent password (skips the forced change flow)
aws cognito-idp admin-set-user-password --user-pool-id us-west-2_KRm2nfNtS --username {username} --password {password} --permanent

### Disable a user
aws cognito-idp admin-disable-user --user-pool-id us-west-2_KRm2nfNtS --username {username}

### Delete a user
aws cognito-idp admin-delete-user --user-pool-id us-west-2_KRm2nfNtS --username {username}

## Prerequisites
- Node.js and npm installed
- AWS CLI installed and configured (aws configure)

## Local Development Setup
1. npm install
2. Copy a site config to public/ (gitignored - must be done manually):
   cp configs/ldsapologetics-Prod.json public/config.json
   cp configs/ldsapologetics-initialRender.html public/initialRender.html
   cp configs/ldsapologetics-BaseStyles.css public/BaseStyles.css
   cp configs/ldsapologetics-favicon.ico public/favicon.ico
   cp configs/ldsapologetics-headerImage.jpg public/headerImage.jpg
3. npm start

## configs/ Pattern
The following public/ files are gitignored and must be copied from configs/ before running or building:
  public/config.json
  public/initialRender.html
  public/BaseStyles.css
  public/favicon.ico
  public/headerImage.jpg
The source files live in configs/ named {siteName}-{file}. Always copy the right ones before running or building.

## sitemenu.json
Each site has its own sitemenu.json managed directly in S3 — it is excluded from the s3 sync
so deployments never overwrite it. To update a site's menu either:
- Use the admin UI (Menu Manager)
- Or upload directly: aws s3 cp public/sitemenu.json s3://{bucketName}/sitemenu.json
The local public/sitemenu.json is only used for local development.

## Adding a New Site
1. Create configs/{siteName}-Prod.json (copy config.example.json as a starting point)
2. Create configs/{siteName}-initialRender.html
3. Create configs/{siteName}-BaseStyles.css
4. Add configs/{siteName}-favicon.ico (binary - provide the actual .ico file)
5. Add configs/{siteName}-headerImage.jpg (binary - provide the actual image)
6. Add cp commands to README under "copy the proper configuration"
7. Add aws s3 sync command to README under "Sync the build folder"

## AWS Authentication
Ensure AWS credentials are active before running any aws commands:
aws configure
