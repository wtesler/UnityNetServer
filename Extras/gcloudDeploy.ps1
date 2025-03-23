$PROJECT = "<GCLOUD_PROJECT>"
$IMAGE = "<GCLOUD_IMAGE>"
$SERVICE = "<GCLOUD_SERVICE>"
$REGION = "<GCLOUD_REGION>"

cd $PSScriptRoot

gcloud config set project $PROJECT

gcloud builds submit --tag gcr.io/$PROJECT/$IMAGE
$images = $(gcloud container images list-tags gcr.io/$PROJECT/$IMAGE --format="get(digest)")
$oldImages = $images | Select-Object -Skip 1
foreach ($oldImage in $oldImages) {
    gcloud container images delete gcr.io/$PROJECT/$IMAGE@$($oldImage) --quiet
}

echo "Deploy Image Success"

gcloud run deploy $SERVICE --image gcr.io/$PROJECT/$IMAGE --region $REGION
$inactiveRevisions = $(gcloud run revisions list --region=$REGION --service=$SERVICE --filter="status.conditions.type:Active AND status.conditions.status:'False'" --format='value(metadata.name)')
foreach ($inactiveRevision in $inactiveRevisions) {
    gcloud run revisions delete --region=$REGION $inactiveRevision --quiet
}

echo "Deploy Service Success"
echo "Done"
